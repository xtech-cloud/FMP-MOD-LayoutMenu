
//*************************************************************************************
//   !!! Generated by the fmp-cli 1.85.0.  DO NOT EDIT!
//*************************************************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibMVCS = XTC.FMP.LIB.MVCS;

namespace XTC.FMP.MOD.LayoutMenu.LIB.Unity
{
    /// <summary>
    /// 运行时基类
    /// </summary>
    ///<remarks>
    /// 存储模块运行时创建的对象
    ///</remarks>
    public abstract class MyRuntimeBase
    {
        /// <summary>
        /// ui的根对象
        /// </summary>
        public GameObject rootUI { get; private set; }

        /// <summary>
        /// world的根对象
        /// </summary>
        public GameObject rootWorld { get; private set; }

        /// <summary>
        /// 附件的根对象
        /// </summary>
        public GameObject rootAttachment { get; private set; }

        /// <summary>
        /// ui的实例对象
        /// </summary>
        public GameObject instanceUI { get; private set; }

        /// <summary>
        /// world的实例对象
        /// </summary>
        public GameObject instanceWorld { get; private set; }

        /// <summary>
        /// 实例表，键为实例的uid
        /// </summary>
        public Dictionary<string, MyInstance> instances { get; private set; } = new Dictionary<string, MyInstance>();

        protected MonoBehaviour mono_ { get; set; }
        protected MyConfig config_ { get; set; }
        protected MyCatalog catalog_ { get; set; }
        protected Dictionary<string, LibMVCS.Any> settings_ { get; set; }
        protected LibMVCS.Logger logger_ { get; set; }
        protected MyEntryBase entry_ { get; set; }

        /// <summary>
        /// 模块预加载阶段，预加载到内存中的对象的列表
        /// </summary>
        /// <remarks>
        /// key: 对象的检索路径
        /// object: 对象的实例
        /// </remarks>
        protected Dictionary<string, object> preloads_ { get; set; }

        public MyRuntimeBase(MonoBehaviour _mono, MyConfig _config, MyCatalog _catalog, Dictionary<string, LibMVCS.Any> _settings, LibMVCS.Logger _logger, MyEntryBase _entry)
        {
            mono_ = _mono;
            config_ = _config;
            catalog_ = _catalog;
            settings_= _settings;
            logger_ = _logger;
            entry_ = _entry; 

            // 从设置从取出预加载列表
            LibMVCS.Any anyPreloads;
            if (settings_.TryGetValue("preloads", out anyPreloads))
            {
                preloads_ = anyPreloads.AsObject() as Dictionary<string, object>;
            }
            if (null == preloads_)
            {
                preloads_ = new Dictionary<string, object>();
            }
        }
        
        /// <summary>
        /// 预加载
        /// </summary>
        /// <param name="_onProgress">加载进度的百分比</param>
        /// <param name="_onFinish">加载完成</param>
        public virtual void Preload(System.Action<int> _onProgress, System.Action _onFinish)
        {
            _onProgress(100);
            _onFinish();
        }

        /// <summary>
        /// 处理从UAB中实例化的根对象
        /// </summary>
        /// <param name="_root">根对象</param>
        /// <param name="_uiSlot">ui的挂载槽</param>
        /// <param name="_worldSlot">world的挂载槽</param>
        public virtual void ProcessRoot(GameObject _root, Transform _uiSlot, Transform _worldSlot)
        {
            string attachmentsRootName = string.Format("[Attachments_Root_({0})]", MyEntry.ModuleName);
            var attachmentsRoot = _root.transform.Find(attachmentsRootName);
            if (null == attachmentsRoot)
            {
                logger_.Error("{0} not found", attachmentsRoot);
                return;
            }
            rootAttachment = attachmentsRoot.gameObject;

            string worldRootName = string.Format("[World_Root_({0})]", MyEntry.ModuleName);
            var worldRoot = _root.transform.Find(worldRootName);
            if (null == worldRoot)
            {
                logger_.Error("{0} not found", worldRoot);
                return;
            }
            rootWorld = worldRoot.gameObject;

            // 将世界挂载到指定的槽
            rootWorld.transform.SetParent(_worldSlot);
            // 挂载后重置参数
            rootWorld.transform.localScale = Vector3.one;
            rootWorld.transform.localRotation = Quaternion.identity;
            rootWorld.transform.localPosition = Vector3.zero;
            rootWorld.SetActive(config_.world.visible);

            string uiRootName = string.Format("Canvas/[UI_Root_({0})]", MyEntry.ModuleName);
            var uiRoot = _root.transform.Find(uiRootName);
            if (null == uiRoot)
            {
                logger_.Error("{0} not found", uiRoot);
                return;
            }

            // 将ui挂载到指定的槽上
            rootUI = uiRoot.gameObject;
            rootUI.transform.SetParent(_uiSlot);
            // 挂载后重置参数
            rootUI.transform.localScale = Vector3.one;
            rootUI.transform.localRotation = Quaternion.identity;
            rootUI.transform.localPosition = Vector3.zero;
            RectTransform rt = rootUI.GetComponent<RectTransform>();
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rootUI.SetActive(config_.ui.visible);

            // 隐藏根对象
            _root.gameObject.SetActive(false);
            _root.name = _root.name + MyEntryBase.ModuleName;

            // 查找ui实例的对象
            var rInstanceUi = rootUI.transform.Find("instance");
            if (null == rInstanceUi)
            {
                logger_.Error("uiInstance of {0} not found!", _root.name);
            }
            instanceUI = rInstanceUi.gameObject;
            // 不显示模板
            instanceUI.gameObject.SetActive(false);

            // 查找world实例的对象
            var rInstanceWorld = rootWorld.transform.Find("instance");
            if (null == rInstanceWorld)
            {
                logger_.Error("worldInstance of {0} not found!", _root.name);
            }
            instanceWorld = rInstanceWorld.gameObject;
            // 不显示模板
            instanceWorld.gameObject.SetActive(false);
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        /// <param name="_style">使用的样式名</param>
        /// <param name="_uiRoot">ui挂载的根对象，需要可见</param>
        /// <param name="_uiSlot">ui在根对象下挂载的路径</param>
        /// <param name="_worldRoot">world挂载的根对象，需要可见</param>
        /// <param name="_worldSlot">world在根对象下挂载的路径</param>
        /// <returns></returns>
        public virtual void CreateInstanceAsync(string _uid, string _style, string _uiRoot, string _uiSlot, string _worldRoot, string _worldSlot, System.Action<MyInstance> _onFinish)
        {
            mono_.StartCoroutine(createInstanceAsync(_uid, _style, _uiRoot, _uiSlot, _worldRoot, _worldSlot, _onFinish));
        }

        /// <summary>
        /// 删除实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        public virtual void DeleteInstanceAsync(string _uid)
        {
            mono_.StartCoroutine(deleteInstanceAsync(_uid));
        }


        /// <summary>
        /// 打开实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        /// <param name="_source">内容的源类型</param>
        /// <param name="_uri">内容的地址</param>
        /// <param name="_delay">延时时间，单位秒</param>
        public virtual void OpenInstanceAsync(string _uid, string _source, string _uri, float _delay)
        {
            mono_.StartCoroutine(openInstanceAsync(_uid, _source, _uri, _delay));
        }

        /// <summary>
        /// 显示实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        /// <param name="_delay">延时时间，单位秒</param>
        public virtual void ShowInstanceAsync(string _uid, float _delay)
        {
            mono_.StartCoroutine(showInstanceAsync(_uid, _delay));
        }

        /// <summary>
        /// 隐藏实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        /// <param name="_delay">延时时间，单位秒</param>
        public virtual void HideInstanceAsync(string _uid, float _delay)
        {
            mono_.StartCoroutine(hideInstanceAsync(_uid, _delay));
        }

        /// <summary>
        /// 关闭实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        /// <param name="_delay">延时时间，单位秒</param>
        public virtual void CloseInstanceAsync(string _uid, float _delay)
        {
            mono_.StartCoroutine(closeInstanceAsync(_uid, _delay));
        }

        protected IEnumerator delayDo(float _time, System.Action _action)
        {
            if (0 == _time)
                yield return new WaitForEndOfFrame();
            else
                yield return new WaitForSeconds(_time);
            _action();
        }


        private IEnumerator createInstanceAsync(string _uid, string _style, string _uiRoot, string _uiSlot, string _worldRoot, string _worldSlot, System.Action<MyInstance> _onFinish)
        {
            logger_.Debug("create instance of {0}, uid is {1}, style is {2}, uiRoot is {3}, uiSlot is {4}, worldRoot is {5}, worldSlot is {6}", MyEntryBase.ModuleName, _uid, _style, _uiRoot, _uiSlot, _worldRoot, _worldSlot);
            // 延时一帧执行，在发布消息时不能动态注册
            yield return new WaitForEndOfFrame();

            MyInstance instance;
            if (instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance is exists");
                yield break;
            }

            instance = new MyInstance(_uid, _style, config_, catalog_, logger_, settings_, entry_, mono_, rootAttachment);
            instance.preloadsRepetition = new Dictionary<string, object>(preloads_);
            instances[_uid] = instance;

            // 实例化ui
            // 默认的挂载点为instance模板的父对象
            Transform uiSlot = instanceUI.transform.parent;
            if (!string.IsNullOrEmpty(_uiRoot))
            {
                var uiRoot = GameObject.Find(_uiRoot);
                if (null == uiRoot)
                {
                    logger_.Error("uiRoot {0} not found", _uiRoot);
                    yield break;
                }
                uiSlot = uiRoot.transform;
            }
            if (!string.IsNullOrEmpty(_uiSlot))
            {
                uiSlot = uiSlot.Find(_uiSlot);
                if (null == uiSlot)
                {
                    logger_.Error("uiSlot {0} not found", _uiSlot);
                    yield break;
                }
            }
            instance.InstantiateUI(instanceUI, uiSlot);

            // 实例化world
            Transform worldSlot = instanceWorld.transform.parent;
            if (!string.IsNullOrEmpty(_worldRoot))
            {
                var worldRoot = GameObject.Find(_worldRoot);
                if (null == worldRoot)
                {
                    logger_.Error("worldRoot {0} not found", _worldRoot);
                    yield break;
                }
                worldSlot = worldRoot.transform;
            }
            if (!string.IsNullOrEmpty(_worldSlot))
            {
                worldSlot = worldSlot.Find(_worldSlot);
                if (null == worldSlot)
                {
                    logger_.Error("worldSlot {0} not found", _worldSlot);
                    yield break;
                }
            }
            instance.InstantiateWorld(instanceWorld, worldSlot);

            instance.themeObjectsPool.Prepare();
            instance.HandleCreated();
            // 动态注册直系的MVCS
            entry_.DynamicRegister(_uid, logger_);
            instance.SetupBridges();
            _onFinish(instance);
        }

        private IEnumerator deleteInstanceAsync(string _uid)
        {
            logger_.Debug("delete instance of {0}, uid is {1}", MyEntryBase.ModuleName, _uid);
            // 延时一帧执行，在发布消息时不能动态注销
            yield return new WaitForEndOfFrame();

            MyInstance instance;
            if (!instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance not found");
                yield break;
            }

            instance.HandleDeleted();
            GameObject.Destroy(instance.rootUI);
            GameObject.Destroy(instance.rootWorld);
            GameObject.Destroy(instance.rootAttachments);
            instances.Remove(_uid);
            instance.themeObjectsPool.Dispose();

            // 动态注销直系的MVCS
            entry_.DynamicCancel(_uid, logger_);
        }

        private IEnumerator openInstanceAsync(string _uid, string _source, string _uri, float _delay)
        {
            logger_.Debug("open instance of {0}, uid is {1}", MyEntryBase.ModuleName, _uid);

            yield return new WaitForSeconds(_delay);
            MyInstance instance;
            if (!instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance not found");
                yield break;
            }
            instance.contentObjectsPool.Prepare();
            instance.HandleOpened(_source, _uri);
        }

        private IEnumerator showInstanceAsync(string _uid, float _delay)
        {
            logger_.Debug("show instance of {0}, uid is {1}", MyEntryBase.ModuleName, _uid);

            yield return new WaitForSeconds(_delay);
            MyInstance instance;
            if (!instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance not found");
                yield break;
            }
            instance.HandleShowed();
        }

        private IEnumerator hideInstanceAsync(string _uid, float _delay)
        {
            logger_.Debug("hide instance of {0}, uid is {1}", MyEntryBase.ModuleName, _uid);

            yield return new WaitForSeconds(_delay);
            MyInstance instance;
            if (!instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance not found");
                yield break;
            }
            instance.HandleHided();
        }

        private IEnumerator closeInstanceAsync(string _uid, float _delay)
        {
            logger_.Debug("close instance of {0}, uid is {1}", MyEntryBase.ModuleName, _uid);

            yield return new WaitForSeconds(_delay);
            MyInstance instance;
            if (!instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance not found");
                yield break;
            }
            instance.HandleClosed();
            instance.contentObjectsPool.Dispose();
        }

    }
}
