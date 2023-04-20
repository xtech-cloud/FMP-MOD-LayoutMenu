

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.LayoutMenu.LIB.Proto;
using XTC.FMP.MOD.LayoutMenu.LIB.MVCS;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System;
using Unity.Collections.LowLevel.Unsafe;
using SoftMasking;

namespace XTC.FMP.MOD.LayoutMenu.LIB.Unity
{
    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        public class UiReference
        {
            public RectTransform rtPanel;
            public GameObject templateCell;
            public GameObject objPagination;
            public Button btnPrev;
            public Button btnNext;
            public Text textNumber;
        }

        private UiReference uiReference_ = new UiReference();
        private ContentReader contentReader_ = null;
        private List<string> contentUriS_ = new List<string>();
        private int currentPage_ = 0;
        private int totalPage_ = 0;
        private int pageCipacity_ = 0;
        private List<GameObject> cellS_ = new List<GameObject>();
        private Dictionary<string, string> resourceUriS_ = new Dictionary<string, string>();

        public MyInstance(string _uid, string _style, MyConfig _config, MyCatalog _catalog, LibMVCS.Logger _logger, Dictionary<string, LibMVCS.Any> _settings, MyEntryBase _entry, MonoBehaviour _mono, GameObject _rootAttachments)
            : base(_uid, _style, _config, _catalog, _logger, _settings, _entry, _mono, _rootAttachments)
        {
        }

        /// <summary>
        /// 当被创建时
        /// </summary>
        /// <remarks>
        /// 可用于加载主题目录的数据
        /// </remarks>
        public void HandleCreated()
        {
            uiReference_.rtPanel = rootUI.transform.Find("Panel").GetComponent<RectTransform>();
            uiReference_.templateCell = rootUI.transform.Find("Panel/Cell").gameObject;
            uiReference_.templateCell.SetActive(false);
            loadTextureFromTheme(style_.background.image, (_texture) =>
            {
                rootUI.transform.Find("Background").GetComponent<RawImage>().texture = _texture;
            }, () => { });
            // 应用SoftMask
            {
                var softmask = uiReference_.templateCell.transform.Find("IconMask").gameObject.AddComponent<SoftMask>();
                softmask.defaultUIShader = rootAttachments.transform.Find("Shader_SoftMask").GetComponent<MeshRenderer>().material.shader;
                softmask.defaultUIETC1Shader = rootAttachments.transform.Find("Shader_SoftMaskETC1").GetComponent<MeshRenderer>().material.shader;
                loadTextureFromTheme(style_.cell.iconMask.image, (_texture) =>
                {
                    uiReference_.templateCell.transform.Find("IconMask").GetComponent<RawImage>().texture = _texture;
                }, () => { });
                alignByAncor(softmask.transform, style_.cell.iconMask.anchor);
            }
            // 应用节点图标样式
            {
                var cellIcon = rootUI.transform.Find("Panel/Cell/IconMask/icon").GetComponent<RectTransform>();
                MyConfig.Anchor anchor = new MyConfigBase.Anchor();
                anchor.width = style_.cell.width;
                anchor.height = style_.cell.height;
                alignByAncor(cellIcon.transform, anchor);
            }
            // 应用节点文字样式
            {
                var cellText = rootUI.transform.Find("Panel/Cell/text").GetComponent<RectTransform>();
                alignByAncor(cellText.transform, style_.cell.label.anchor);
                var text = cellText.GetComponent<Text>();
                text.fontSize = style_.cell.label.fontSize;
                text.font = settings_["font.main"].AsObject() as Font;
                Color color = Color.black;
                ColorUtility.TryParseHtmlString(style_.cell.label.color, out color);
                text.color = color;
            }
            // 应用分页栏样式
            {
                uiReference_.objPagination = rootUI.transform.Find("Pagination").gameObject;
                alignByAncor(uiReference_.objPagination.transform, style_.pagination.anchor);
                uiReference_.objPagination.SetActive(false);
                uiReference_.btnPrev = uiReference_.objPagination.transform.Find("btnPrev").GetComponent<Button>();
                uiReference_.btnNext = uiReference_.objPagination.transform.Find("btnNext").GetComponent<Button>();
                uiReference_.textNumber = uiReference_.objPagination.transform.Find("txtNumber").GetComponent<Text>();
                uiReference_.btnPrev.GetComponent<RectTransform>().sizeDelta = new Vector2(style_.pagination.btnPrev.width, style_.pagination.btnPrev.height);
                uiReference_.btnNext.GetComponent<RectTransform>().sizeDelta = new Vector2(style_.pagination.btnNext.width, style_.pagination.btnNext.height);
                uiReference_.textNumber.GetComponent<RectTransform>().sizeDelta = new Vector2(style_.pagination.txtNumber.width, style_.pagination.txtNumber.height);
                loadTextureFromTheme(style_.pagination.btnPrev.image, (_texture) =>
                {
                    uiReference_.btnPrev.GetComponent<RawImage>().texture = _texture;
                }, () => { });
                loadTextureFromTheme(style_.pagination.btnNext.image, (_texture) =>
                {
                    uiReference_.btnNext.GetComponent<RawImage>().texture = _texture;
                }, () => { });
                Color color = Color.black;
                ColorUtility.TryParseHtmlString(style_.pagination.txtNumber.color, out color);
                var text = uiReference_.textNumber.GetComponent<Text>();
                text.color = color;
                text.fontSize = style_.pagination.txtNumber.fontSize;
                uiReference_.btnNext.onClick.AddListener(() =>
                {
                    currentPage_ += 1;
                    if (currentPage_ >= totalPage_)
                        currentPage_ = totalPage_ - 1;
                    refreshPagination();
                });
                uiReference_.btnPrev.onClick.AddListener(() =>
                {
                    currentPage_ -= 1;
                    if (currentPage_ < 0)
                        currentPage_ = 0;
                    refreshPagination();
                });
            }
            buildLayout();
        }

        /// <summary>
        /// 当被删除时
        /// </summary>
        public void HandleDeleted()
        {
        }

        /// <summary>
        /// 当被打开时
        /// </summary>
        /// <remarks>
        /// 可用于加载内容目录的数据
        /// </remarks>
        public void HandleOpened(string _source, string _uri)
        {
            contentReader_ = new ContentReader(contentObjectsPool);
            contentReader_.AssetRootPath = settings_["path.assets"].AsString();
            currentPage_ = 0;
            fillCells();
            rootUI.gameObject.SetActive(true);
            rootWorld.gameObject.SetActive(true);
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void HandleClosed()
        {
            rootUI.gameObject.SetActive(false);
            rootWorld.gameObject.SetActive(false);
            contentReader_ = null;

            foreach (var obj in cellS_)
            {
                GameObject.Destroy(obj.gameObject);
            }
            cellS_.Clear();
            contentUriS_.Clear();
            resourceUriS_.Clear();
        }

        private void buildLayout()
        {
            if (style_.layout == "GridLayout")
            {
                alignByAncor(uiReference_.rtPanel.transform, style_.gridLayout.anchor);
                var layout = uiReference_.rtPanel.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(style_.cell.width, style_.cell.height);
                layout.spacing = new Vector2(style_.gridLayout.spacing.x, style_.gridLayout.spacing.y);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = style_.gridLayout.column;
                pageCipacity_ = style_.gridLayout.row * style_.gridLayout.column;
            }
        }

        private void fillCells()
        {

            // 开始解析包含的包
            Dictionary<string, List<string>> bundleUuidS = new Dictionary<string, List<string>>();
            foreach (var section in catalog_.sectionS)
            {
                if (!new List<string>(section.instanceS).Contains(uid))
                    continue;
                foreach (var content in section.contentS)
                {
                    var valS = content.Split("/");
                    if (valS.Length != 2)
                        continue;
                    if (!bundleUuidS.ContainsKey(valS[0]))
                        bundleUuidS.Add(valS[0], new List<string>());
                    if (!bundleUuidS[valS[0]].Contains(content))
                        bundleUuidS[valS[0]].Add(content);
                }
            }

            // 包加载的计数器序列
            CounterSequence bundleLoadSequence = new CounterSequence(bundleUuidS.Count);
            bundleLoadSequence.OnFinish = () =>
            {
                logger_.Info("found [{0}] contents", contentUriS_.Count);
                totalPage_ = contentUriS_.Count / (style_.gridLayout.row * style_.gridLayout.column);
                if (contentUriS_.Count % (style_.gridLayout.row * style_.gridLayout.column) != 0)
                    totalPage_ += 1;
                refreshPagination();
                uiReference_.objPagination.SetActive(totalPage_ > 1);
            };

            // 开始解析匹配的内容路径
            foreach (var bundleUuid in bundleUuidS.Keys)
            {
                contentReader_.ContentUri = bundleUuid;
                contentReader_.LoadText("meta.json", (_bytes) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(_bytes);
                        var bundle = JsonConvert.DeserializeObject<BundleMetaSchema>(json);
                        foreach (var contentUuid in bundle.foreign_content_uuidS)
                        {
                            string contentUri = string.Format("{0}/{1}", bundleUuid, contentUuid);
                            var contentPatternS = bundleUuidS[bundleUuid];
                            foreach (var pattern in contentPatternS)
                            {
                                if (BundleMetaSchema.IsMatch(contentUri, pattern))
                                {
                                    contentUriS_.Add(contentUri);
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger_.Error("load:{0}/meta.json throw exception", bundleUuid);
                        logger_.Exception(ex);
                    }
                    bundleLoadSequence.Tick();
                }, () => { });
            }
        }

        private void refreshPagination()
        {
            uiReference_.textNumber.text = string.Format("{0}/{1}", currentPage_ + 1, totalPage_);
            uiReference_.btnPrev.interactable = currentPage_ > 0;
            uiReference_.btnNext.interactable = currentPage_ < totalPage_ - 1;

            // 创建节点的函数定义
            Func<string, GameObject> createCell = (_contentUri) =>
            {
                var clone = GameObject.Instantiate(uiReference_.templateCell, uiReference_.templateCell.transform.parent);
                clone.name = _contentUri;
                clone.gameObject.SetActive(true);
                contentReader_.ContentUri = _contentUri;
                contentReader_.LoadText("meta.json", (_bytes) =>
                {
                    try
                    {
                        string json = Encoding.UTF8.GetString(_bytes);
                        var contentSchema = JsonConvert.DeserializeObject<ContentMetaSchema>(json);
                        clone.transform.Find("text").GetComponent<Text>().text = contentSchema.name;
                        string kvValue = null;
                        if (contentSchema.kvS.TryGetValue(style_.metaKey, out kvValue))
                        {
                            resourceUriS_[_contentUri] = string.Format("{0}/_resources/{1}", contentSchema.foreign_bundle_uuid, kvValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger_.Error("load:{0}/meta.json throw exception", _contentUri);
                        logger_.Exception(ex);
                    }
                }, () => { });
                contentReader_.LoadTexture("icon.png", (_texture) =>
                {
                    clone.transform.Find("IconMask/icon").GetComponent<RawImage>().texture = _texture;
                }, () => { });
                clone.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Dictionary<string, object> variableS = new Dictionary<string, object>();
                    if (resourceUriS_.ContainsKey(_contentUri))
                    {
                        variableS["{{resource_uri}}"] = resourceUriS_[_contentUri];
                    }
                    publishSubjects(style_.cell.onClickSubjectS, variableS);
                });
                return clone;
            };

            foreach (var obj in cellS_)
            {
                GameObject.Destroy(obj.gameObject);
            }
            cellS_.Clear();

            for (int i = currentPage_ * pageCipacity_; i < contentUriS_.Count && i < (currentPage_ + 1) * pageCipacity_; ++i)
            {
                var contentUri = contentUriS_[i];
                var obj = createCell(contentUri);
                cellS_.Add(obj);
            }
        }
    }
}
