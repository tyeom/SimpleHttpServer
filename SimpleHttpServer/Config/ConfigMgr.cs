using System;
using System.IO;
using System.Collections;
using SimpleHttpServer.Common;
using System.Xml;
using System.Collections.Generic;

namespace SimpleHttpServer.Config
{
    /// <summary>
    /// 부모코드
    /// </summary>
    public enum ParentCode
    {
        /// <summary>
        /// 기본설정
        /// </summary>
        Base,
        /// <summary>
        /// 문서
        /// </summary>
        Document,
        /// <summary>
        /// 기타
        /// </summary>
        Etc,
    }

    /// <summary>
    /// 코드
    /// [GEI(부모코드, "환경설정 초기 값")]
    /// </summary>
    public enum ChildCode
    {
        #region 기본설정
        [GEI(ParentCode.Base, "C:\\HttpServer\\wwwroot")]
        DefaultRootDirectory,
        [GEI(ParentCode.Base, "8080")]
        DefaultPort,
        #endregion  // 기본설정

        #region 문서
        /// <summary>
        /// 기본문서
        /// </summary>
        [GEI(ParentCode.Document, "{Default.html||Index.html}")]
        DefaultDocument,
        #endregion  // 문서

        #region 기타
        /// <summary>
        /// 기타1
        /// </summary>
        [GEI(ParentCode.Etc, "aaa")]
        Etc1,
        /// <summary>
        /// 기타2
        /// </summary>
        [GEI(ParentCode.Etc, "bbb")]
        Etc2,
        #endregion  // 기타
    }

    public class ConfigMgr
    {
        private const string _ConfigXmlFileName = "ServerSetting.xml";
        private const string _XMLRootNodeName = "ServerSetting";
        private string _SettingXML = null;
        private Hashtable _SettingTable = null;
        private bool _isFileSave = false;

        #region _SINGLE_TONE_
        public ConfigMgr()
        {
            this.Init();
        }

        private static ConfigMgr _default = null;
        public static ConfigMgr Default
        {
            get
            {
                if (_default == null) _default = new ConfigMgr();

                return _default;
            }
        }
        #endregion  // _SINGLE_TONE_

        private void Init()
        {
            _SettingTable = new Hashtable();

            if (File.Exists(_ConfigXmlFileName))
            {
                _SettingXML = File.ReadAllText(_ConfigXmlFileName);
            }
            else
            {
                _SettingXML = null;
            }

            if (string.IsNullOrEmpty(_SettingXML))
            {
                _SettingXML = "<" + _XMLRootNodeName + "></" + _XMLRootNodeName + ">";
                Array Childs = Enum.GetValues(typeof(ChildCode));
                for (int i = 0; i < Childs.Length; i++)
                {
                    ChildCode Child = (ChildCode)Childs.GetValue(i);
                    ParentCode ParentEnum = (ParentCode)GEI.GetParameter<ChildCode>(Child, 0);
                    string ChildVal = GEI.GetParameter<ChildCode>(Child, 1).ToString();
                    this.SetValueByEnum(ParentEnum, Child, ChildVal);
                }

                // Update
                File.WriteAllText(_ConfigXmlFileName, _SettingXML);
            }
        }

        /// <summary>
        /// 해당 환경설정 값 로컬 저장
        /// </summary>
        /// <param name="parentCode">환경설정 부모코드</param>
        /// <param name="childCode">환경설정 자식코드</param>
        /// <param name="Value">환경설정 값</param>
        public void SetValueByEnum(ParentCode parentCode, ChildCode childCode, object Value)
        {
            _isFileSave = false;
            ParentCode Child_Parent = (ParentCode)GEI.GetParameter<ChildCode>(childCode, 0);
            if (Child_Parent == parentCode)
            {
                this.SetValue(parentCode.ToString(), childCode.ToString(), Value);
            }
            else
            {
                throw new Exception("부모 코드와 자식 코드가 매칭되지 않습니다.");
            }
        }

        /// <summary>
        /// 해당 환경설정 값 가져오기
        /// </summary>
        /// <param name="parentCode">환경설정 부모코드</param>
        /// <param name="childCode">환경설정 자식코드</param>
        /// <returns>환경설정 값</returns>
        public object GetValueByEnum(ParentCode parentCode, ChildCode childCode)
        {
            ParentCode Child_Parent = (ParentCode)GEI.GetParameter<ChildCode>(childCode, 0);

            if (Child_Parent == parentCode)
            {
                object ReturnObj = this.GetValue(parentCode.ToString(), childCode.ToString());
                // 사용자가 환경설정 값 저장을 안한 경우
                if (ReturnObj == null)
                {
                    // 기본 환경설정 값
                    string DefaultVal = GEI.GetParameter<ChildCode>(childCode, 1).ToString();
                    // 기본 환경설정 값 저장
                    this.UpdateValueByEnum(parentCode, childCode, DefaultVal);

                    // 다시 불러옴.
                    return this.GetValue(parentCode.ToString(), childCode.ToString());
                }

                return ReturnObj;
            }
            else
            {
                throw new Exception("부모 코드와 자식 코드가 매칭되지 않습니다.");
            }
        }

        public void UpdateValueByEnum(ParentCode parentCode, ChildCode childCode, object Value)
        {
            _isFileSave = true;
            ParentCode Child_Parent = (ParentCode)GEI.GetParameter<ChildCode>(childCode, 0);
            if (Child_Parent == parentCode)
            {
                this.SetValue(parentCode.ToString(), childCode.ToString(), Value);
            }
            else
            {
                throw new Exception("부모 코드와 자식 코드가 매칭되지 않습니다.");
            }
            _isFileSave = false;
        }

        #region 값 저장 / 값 불러오기
        private void SetValue(string parentCode, string childCode, object Value)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(_SettingXML);

            XmlNodeList xmlItems = xmlDocument.SelectNodes(_XMLRootNodeName + "/" + parentCode);

            XmlNode root = xmlDocument.DocumentElement;

            Value = this.GetStringItems(Value);
            // 부모 노드가 없는 경우
            if (xmlItems.Count <= 0)
            {
                XmlNode Parent = xmlDocument.CreateElement(parentCode);
                XmlNode Child = xmlDocument.CreateElement(childCode);
                Child.InnerText = Value.ToString();
                Parent.AppendChild(Child);
                root.AppendChild(Parent);
            }
            else
            {
                XmlNode ChildNode = xmlItems[0].SelectSingleNode(childCode);
                // 부모 노드는 있지만 자식노드가 없는 경우   [새로운 자식 노드 추가]
                if (ChildNode == null)
                {
                    XmlNode Child = xmlDocument.CreateElement(childCode);
                    Child.InnerText = Value.ToString();
                    xmlItems[0].AppendChild(Child);
                }
                else
                {
                    xmlItems[0].SelectSingleNode(childCode).InnerText = Value.ToString();
                }
            }

            // 환경설정 로컬 해시테이블에 저장
            this.LocalHashTableSet(parentCode, childCode, Value);

            _SettingXML = xmlDocument.InnerXml;

            if (_isFileSave)
            {
                // Update
                File.WriteAllText(_ConfigXmlFileName, _SettingXML);
            }
        }

        private object GetValue(string parentCode, string childCode)
        {
            bool LocalHashTableSaveFlag = false;

            object ObjVal = this.GetValueByLocalHashTable(parentCode, childCode);
            // 로컬 해시테이블에 값이 있는 경우 반환.
            if (ObjVal == null)
            {
                LocalHashTableSaveFlag = true;

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(_SettingXML);

                XmlNodeList xmlItems = xmlDocument.SelectNodes(_XMLRootNodeName + "/" + parentCode);
                if (xmlItems != null && xmlItems.Count > 0)
                {
                    XmlNode Node = xmlItems[0].SelectSingleNode(childCode);
                    if (Node != null)
                    {
                        ObjVal = Node.InnerText;
                    }
                }
            }

            // 배열형태인지 체크
            if (ObjVal != null && ObjVal.ToString().StartsWith("{") && ObjVal.ToString().EndsWith("}"))
            {
                string StrArrayVal = ObjVal.ToString();
                StrArrayVal = StrArrayVal.Remove(0, 1).Remove(StrArrayVal.Length - 2, 1);

                string[] ValToKen = StrArrayVal.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                List<object> ArrayObj = new List<object>();
                foreach (string Token in ValToKen)
                {
                    ArrayObj.Add(Token);
                }

                ObjVal = ArrayObj.ToArray();
            }

            // 로컬 해시테이블에 저장.
            if (LocalHashTableSaveFlag)
                this.LocalHashTableSet(parentCode, childCode, ObjVal);

            return ObjVal;
        }

        public void SaveOrUpdate()
        {
            // Update
            File.WriteAllText(_ConfigXmlFileName, _SettingXML);
        }
        #endregion

        /// <summary>
        /// 리스트나 배열 타입의 요소를 문자열로 반환한다.
        /// </summary>
        /// <param name="Array"></param>
        /// <returns></returns>
        private string GetStringItems(object ArrayObj)
        {
            string ResultStrItems = null;
            if (ArrayObj is Array || ArrayObj.GetType().IsGenericType)  // 배열 타입 또는 List<T>타입
            {
                if (ArrayObj is System.Collections.IEnumerable)
                {
                    ResultStrItems = "{";
                    foreach (object Item in (System.Collections.IEnumerable)ArrayObj)
                    {
                        ResultStrItems += Item + "||";
                    }
                    ResultStrItems += "}";
                }
            }
            else
            {
                ResultStrItems = ArrayObj.ToString();
            }

            return ResultStrItems;
        }

        /// <summary>
        /// 환경설정 로컬 해시테이블에 저장
        /// </summary>
        /// <param name="parentCode"></param>
        /// <param name="childCode"></param>
        /// <param name="Value"></param>
        private void LocalHashTableSet(string parentCode, string childCode, object Value)
        {
            string SettingCode = string.Format("{0}\\{1}", parentCode, childCode);
            if (_SettingTable.ContainsKey(SettingCode) == false)
            {
                _SettingTable.Add(SettingCode, Value);
            }
            else
            {
                _SettingTable[SettingCode] = Value;
            }
        }

        private object GetValueByLocalHashTable(string parentCode, string childCode)
        {
            string SettingCode = string.Format("{0}\\{1}", parentCode, childCode);
            if (_SettingTable.ContainsKey(SettingCode) == false)
            {
                return null;
            }
            else
            {
                return _SettingTable[SettingCode];
            }
        }
    }
}
