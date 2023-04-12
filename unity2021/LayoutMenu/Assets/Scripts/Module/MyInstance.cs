

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.LayoutMenu.LIB.Proto;
using XTC.FMP.MOD.LayoutMenu.LIB.MVCS;
using Unity.VisualScripting.Dependencies.NCalc;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System;
using Unity.Collections.LowLevel.Unsafe;

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
        }

        private UiReference uiReference_ = new UiReference();
        private ContentReader contentReader_ = null;
        private List<string> contentUriS_ = new List<string>();

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
            // 应用节点文字样式
            {

                var cellText = rootUI.transform.Find("Panel/Cell/text").GetComponent<RectTransform>();
                cellText.sizeDelta = new Vector2(style_.cell.label.width, style_.cell.label.height);
                cellText.anchoredPosition = new Vector2(0, -style_.cell.label.offset);
                var text = cellText.GetComponent<Text>();
                text.fontSize = style_.cell.label.fontSize;
                Color color = Color.black;
                ColorUtility.TryParseHtmlString(style_.cell.label.color, out color);
                text.color = color;
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
        }

        private void buildLayout()
        {
            if (style_.layout == "GridLayout")
            {
                alignByAncor(uiReference_.rtPanel.transform, style_.gridLayout.anchor);
                var layout = uiReference_.rtPanel.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(style_.cell.width, style_.cell.height);
                layout.padding = new RectOffset(style_.gridLayout.padding.left, style_.gridLayout.padding.right, style_.gridLayout.padding.left, style_.gridLayout.padding.bottom);
                layout.spacing = new Vector2(style_.gridLayout.spacing.x, style_.gridLayout.spacing.y);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = style_.gridLayout.column;
            }
        }

        private void fillCells()
        {
            // 创建节点的函数定义
            Action<string> createCell = (_contentUri) =>
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
                    }
                    catch (Exception ex)
                    {
                        logger_.Error("load:{0}/meta.json throw exception", _contentUri);
                        logger_.Exception(ex);
                    }
                }, () => { });
                contentReader_.LoadTexture("icon.png", (_texture) =>
                {
                    clone.GetComponent<RawImage>().texture = _texture;
                }, () => { });
            };

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
                foreach (var contentUri in contentUriS_)
                {
                    createCell(contentUri);
                }
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
    }
}
