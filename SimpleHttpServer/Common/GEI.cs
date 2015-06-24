using System;
using System.Reflection;

namespace SimpleHttpServer.Common
{
    /// <summary>
    /// Generic Enum Info Class
    /// Enum에 어트리뷰트를 사용하여 정보를 추가한다.
    /// </summary>
    public class GEI : Attribute
    {
        object[] _Objs = null;

        public GEI(params Object[] pParams)
        {
            _Objs = pParams;
        }

        private object GetParameter(int pIndex)
        {
            return _Objs[pIndex];
        }

        public static object GetParameter<T>(T pEnum, int pIndex)
        {
            Type tType = typeof(T);
            FieldInfo FI = tType.GetField(pEnum.ToString());
            GEI tObj = FI.GetCustomAttributes(false)[0] as GEI;

            return tObj.GetParameter(pIndex);
        }
    }
}
