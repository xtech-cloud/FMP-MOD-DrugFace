
using System.Collections.Generic;
using UnityEngine;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.DrugFace.LIB.MVCS;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    /// <summary>
    /// 运行时类
    /// </summary>
    ///<remarks>
    /// 存储模块运行时创建的对象
    ///</remarks>
    public class MyRuntime : MyRuntimeBase
    {
        public MyRuntime(MonoBehaviour _mono, MyConfig _config, Dictionary<string, LibMVCS.Any> _settings, FMP.LIB.MVCS.Logger _logger, MyEntryBase _entry)
            : base(_mono, _config, _settings, _logger, _entry)
        {
        }
    }
}

