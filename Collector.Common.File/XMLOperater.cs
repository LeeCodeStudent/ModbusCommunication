using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Collector.Common.File
{
    public class XMLOperater
    {
        private XmlDocument _xmlDocument;

        private string _filePath;

        private string _rootName;

        public XMLOperater(string filePath, string rootName)
        {
            _filePath = filePath;
            _rootName = rootName;
        }

        public void LoadXml()
        {
            _xmlDocument = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            //如果给定路径文件不存在，则在该路径下创建一个文件
            if (!System.IO.File.Exists(_filePath))
            {
                this.CreateXmlDocument();
            }
            XmlReader xmlReader = XmlReader.Create(_filePath, settings);
            _xmlDocument.Load(xmlReader);
            xmlReader.Close();
        }

        private void CreateXmlDocument()
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode node = _xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
                    _xmlDocument.AppendChild(node);
                    XmlNode rootNode = _xmlDocument.CreateElement(_rootName);
                    _xmlDocument.AppendChild(rootNode);
                    _xmlDocument.Save(_filePath);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public void SetChildNodeAttributs<T>(string childNodeName, IList<T> nodes)
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode rootNode = _xmlDocument.DocumentElement;
                    FieldInfo[] fieldInfos = typeof(T).GetFields();
                    XmlElement newElement = _xmlDocument.CreateElement(childNodeName);
                    foreach (var node in nodes)
                    {
                        foreach (var field in fieldInfos)
                        {
                            newElement.SetAttribute(field.Name, field.GetValue(node).ToString());
                        }
                    }
                    rootNode.AppendChild(newElement);
                    _xmlDocument.Save(_filePath);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SetChildNodeInner<T>(string childNodeName, IList<T> nodes)
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode rootNode = _xmlDocument.DocumentElement;
                    FieldInfo[] fieldInfos = typeof(T).GetFields();
                    XmlElement newElement = _xmlDocument.CreateElement(childNodeName);
                    foreach (var node in nodes)
                    {
                        foreach (var field in fieldInfos)
                        {
                            XmlElement newElementChild = _xmlDocument.CreateElement(field.Name);
                            newElementChild.InnerText = field.GetValue(node).ToString();
                            newElement.AppendChild(newElementChild);
                        }
                    }
                    rootNode.AppendChild(newElement);
                    _xmlDocument.Save(_filePath);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public object GetChildNodeAttribut<T>(string childNodeName)
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode rootNode = _xmlDocument.DocumentElement;
                    XmlNode node = rootNode.SelectSingleNode(childNodeName);
                    XmlElement element = node as XmlElement;
                    object nodeObject = Activator.CreateInstance(typeof(T));
                    FieldInfo[] fieldInfos = typeof(T).GetFields();
                    foreach (var field in fieldInfos)
                    {
                        field.SetValue(nodeObject, Convert.ChangeType(element.GetAttribute(field.Name), field.FieldType));
                    }
                    return nodeObject;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public List<T> GetChildNodeInner<T>(string childNodeName) where T : class
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode rootNode = _xmlDocument.DocumentElement;
                    XmlNodeList nodes = rootNode.SelectNodes(childNodeName);
                    FieldInfo[] fieldInfos = typeof(T).GetFields();
                    List<T> list = new List<T>();
                    foreach (XmlNode node in nodes)
                    {
                        object nodeObject = Activator.CreateInstance(typeof(T));
                        foreach (var field in fieldInfos)
                        {
                            if (field.FieldType.IsEnum)
                            {
                                string innerText = node.SelectSingleNode(field.Name).InnerText;
                                if (innerText != "")
                                    field.SetValue(nodeObject, Enum.Parse(field.FieldType, innerText));
                                else
                                    field.SetValue(nodeObject, null);
                            }
                            else
                                field.SetValue(nodeObject, Convert.ChangeType(node.SelectSingleNode(field.Name).InnerText, field.FieldType));
                        }
                        list.Add(nodeObject as T);
                    }
                    return list;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
        private byte[] GetDataFromString(string dataStr)
        {
            string[] byteStr = dataStr.Split('-');
            List<byte> dataList = new List<byte>();
            foreach (var str in byteStr)
                dataList.Add(Convert.ToByte(str, 16));
            return dataList.ToArray();
        }

        public void ModifyChildNodeAttribut<T>(string childNodeName, T node)
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode rootNode = _xmlDocument.DocumentElement;
                    XmlNode modifyNode = rootNode.SelectSingleNode(childNodeName);
                    FieldInfo[] fieldInfos = typeof(T).GetFields();
                    foreach (var field in fieldInfos)
                    {
                        modifyNode.Attributes[field.Name].Value = field.GetValue(node).ToString();
                    }
                    _xmlDocument.Save(_filePath);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        public void ModifyChildNodeInner<T>(string childNodeName, T node)
        {
            try
            {
                if (_xmlDocument != null)
                {
                    XmlNode rootNode = _xmlDocument.DocumentElement;
                    XmlNode modifyNode = rootNode.SelectSingleNode(childNodeName);

                    FieldInfo[] fieldInfos = typeof(T).GetFields();
                    foreach (var field in fieldInfos)
                    {
                        modifyNode.SelectSingleNode(field.Name).InnerText = field.GetValue(node).ToString();
                    }

                    _xmlDocument.Save(_filePath);
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
    }
}
