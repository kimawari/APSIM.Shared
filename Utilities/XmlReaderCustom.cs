
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Implements an XmlReader for allowing derived custom readers to be created more easily.
    /// </summary>
    /// <remarks>
    /// The idea and inspiration for this came from Ralf Westphal's artical on MSDN:
    /// https://msdn.microsoft.com/en-us/library/ms973822.aspx
    /// I couldn't find the source for this article so I wrote my own.
    /// </remarks>
    public abstract class XmlReaderCustom : XmlReader
    {
        /// <summary>The current attribute index. -1 if not reading attributes.</summary>
        private int currentAttributeIndex = -1;

        /// <summary>The current type of XML node being read.</summary>
        XmlNodeType nodeType = XmlNodeType.None;

        /// <summary>The internal stack of elements.</summary>
        private Stack<CustomElement> elements = new Stack<CustomElement>();

        /// <summary>True if currently reading attributes.</summary>
        private bool readingAttributeValue = false;

        /// <summary>
        /// An element node that 'GetNextElement' creates and returns. It is added to an
        /// internal stack.
        /// </summary>
        protected class CustomElement
        {
            /// <summary>The name of the XML element to create.</summary>
            public string Name;

            /// <summary>The value of the XML element - can be null for text nodes.</summary>
            public string Value;

            /// <summary>The attributes of the element</summary>
            public List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
        }

        /// <summary>Gets the next element.</summary>
        /// <returns>The element or null if an end element.</returns>
        protected abstract CustomElement GetNextElement();

        /// <summary>Constructor</summary>
        public XmlReaderCustom()
        {
        }

        /// <summary>Reads the next node from the stream.</summary>
        /// <returns>True if node was read.</returns>
        public override bool Read()
        {
            if (elements.Count > 0 && elements.Peek().Name == string.Empty)
                elements.Pop(); // get rid of value nodes.

            CustomElement element = GetNextElement();
            if (element == null)
            {
                if (elements.Count > 0)
                    elements.Pop();
                nodeType = XmlNodeType.EndElement;
            }
            else
            {
                elements.Push(element);
                if (element.Name == string.Empty)
                    nodeType = XmlNodeType.Text;
                else
                    nodeType = XmlNodeType.Element;
            }

            return elements.Count > 0;
        }

        /// <summary>Gets the number of attributes.</summary>
        public override int AttributeCount
        {
            get
            {
                return elements.Peek().attributes.Count;
            }
        }

        /// <summary>Gets the value of the attribute with the specified Name.</summary>
        /// <param name="name">The attribute name.</param>
        /// <returns>Attribute value or null if not found.</returns>
        public override string GetAttribute(string name)
        {
            foreach (KeyValuePair<string, string> attribute in elements.Peek().attributes)
            {
                string key = attribute.Key;
                if (key.Contains(':'))
                    key = key.Substring(key.IndexOf(':') + 1);
                if (key == name)
                    return attribute.Value;
            }
            return null;
        }

        /// <summary>Gets the value of the attribute with the specified Name.</summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="namespaceURI">The namespace URI</param>
        /// <returns>Attribute value or null if not found.</returns>
        public override string GetAttribute(string name, string namespaceURI)
        {
            return GetAttribute(name);
        }

        /// <summary>Gets the value of the attribute with the specified index.</summary>
        /// <param name="i">The index of the attribute.</param>
        /// <returns>Attribute value or null if not found.</returns>
        public override string GetAttribute(int i)
        {
            if (i >= 0 && i < elements.Peek().attributes.Count)
                return elements.Peek().attributes[i].Value;
            else
                return null;
        }

        /// <summary>Moves to the attribute with the specified Name.</summary>
        /// <param name="name">The attribute to find.</param>
        /// <returns>True if found.</returns>
        public override bool MoveToAttribute(string name)
        {
            for (int i = 0; i < elements.Peek().attributes.Count; i++)
            {
                string key = elements.Peek().attributes[i].Key;
                if (key.Contains(':'))
                    key = key.Substring(key.IndexOf(':') + 1);
                if (key == name)
                {
                    currentAttributeIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Moves to the attribute with the specified LocalName and NamespaceURI.</summary>
        /// <param name="name">The attribute name.></param>
        /// <param name="ns">The namespace URI</param>
        /// <returns>True if found.</returns>
        public override bool MoveToAttribute(string name, string ns)
        {
            return MoveToAttribute(name);
        }

        /// <summary>Moves to the first attribute.</summary>
        /// <returns>True if found.</returns>
        public override bool MoveToFirstAttribute()
        {
            if (elements.Peek().attributes.Count > 0)
            {
                currentAttributeIndex = 0;
                nodeType = XmlNodeType.Attribute;
                return true;
            }
            return false;
        }

        /// <summary>Move to the next attribute.</summary>
        /// <returns>True if OK.</returns>
        public override bool MoveToNextAttribute()
        {
            if (currentAttributeIndex + 1 < elements.Peek().attributes.Count)
            {
                nodeType = XmlNodeType.Attribute;
                currentAttributeIndex++;
                return currentAttributeIndex < elements.Peek().attributes.Count;
            }
            else
                return false;
        }

        /// <summary>Moves to the element that contains the current attribute node.</summary>
        /// <returns>True if OK</returns>
        public override bool MoveToElement()
        {
            if (nodeType == XmlNodeType.Attribute)
                nodeType = XmlNodeType.Element;
            currentAttributeIndex = -1;
            return true;
        }

        /// <summary>Read the attribute value.</summary>
        /// <returns>True if OK.</returns>
        public override bool ReadAttributeValue()
        {
            readingAttributeValue = !readingAttributeValue;
            if (readingAttributeValue)
                nodeType = XmlNodeType.Text;
            else
                nodeType = XmlNodeType.Attribute;
            return readingAttributeValue;
        }

        /// <summary>Close the reader.</summary>
        public override void Close()
        {
        }

        /// <summary>Resolves a namespace prefix in the current element's scope.</summary>
        /// <param name="prefix">The Prefix</param>
        /// <returns></returns>
        public override string LookupNamespace(string prefix)
        {
            throw new NotImplementedException();
            //return reader.LookupNamespace(prefix);
        }

        /// <summary>Resolves the entity reference for EntityReference nodes.</summary>
        public override void ResolveEntity()
        {
            throw new NotImplementedException();
        }

        /// <summary>Gets the node type.</summary>
        public override XmlNodeType NodeType
        {
            get
            {
                return nodeType;
            }
        }

        /// <summary>Gets the local name of the current element</summary>
        public override string LocalName
        {
            get
            {
                if (elements.Count == 0)
                    return string.Empty;

                if (currentAttributeIndex == -1)
                    return elements.Peek().Name;
                else
                    return elements.Peek().attributes[currentAttributeIndex].Key;
            }
        }

        /// <summary>Gets the namespace URI</summary>
        public override string NamespaceURI
        {
            get
            {
                throw new NotImplementedException();
                //return reader.NamespaceURI;
            }
        }

        /// <summary>Gets the prefix of the current element.</summary>
        public override string Prefix
        {
            get
            {
                if (currentAttributeIndex != -1)
                {
                    string key = elements.Peek().attributes[currentAttributeIndex].Key;
                    if (key.Contains(':'))
                        return key.Substring(0, key.IndexOf(':'));
                }
                return string.Empty;
            }
        }

        /// <summary>Gets the value of the current element.</summary>
        public override string Value
        {
            get
            {
                if (currentAttributeIndex != -1)
                    return elements.Peek().attributes[currentAttributeIndex].Value;
                else
                    return elements.Peek().Value;
            }
        }

        /// <summary>Get the depth of the current element.</summary>
        public override int Depth
        {
            get
            {
                return elements.Count;
            }
        }

        /// <summary>Gets the base URI</summary>
        public override string BaseURI
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>Returns true if element is empty.</summary>
        public override bool IsEmptyElement
        {
            get
            {
                return false;
            }
        }

        /// <summary>Returns true if at end of file.</summary>
        public override bool EOF
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Returns the state of the reader.</summary>
        public override ReadState ReadState
        {
            get
            {
                return ReadState.Interactive;
            }
        }

        /// <summary>Returns the name table.</summary>
        public override XmlNameTable NameTable
        {
            get
            {
                throw new NotImplementedException();
                //return reader.NameTable;
            }
        }

    }
}
