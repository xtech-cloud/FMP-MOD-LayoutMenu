
//*************************************************************************************
//   !!! Generated by the fmp-cli 1.85.0.  DO NOT EDIT!
//*************************************************************************************

using System.Threading;
using System.Threading.Tasks;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.MOD.LayoutMenu.LIB.Bridge
{
    /// <summary>
    /// Healthy的视图桥接层（协议部分）
    /// 处理UI的事件
    /// </summary>
    public interface IHealthyViewProtoBridge : View.Facade.Bridge
    {

        /// <summary>
        /// 处理Echo的提交
        /// </summary>
        Task<Error> OnEchoSubmit(IDTO _dto, object? _context);


    }
}

