using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Hl7.Fhir.Navigation
{
    internal class XElementConversionStrategy : INodeConversionStrategy<XObject>
    {
        public static readonly XName XVALUE = XName.Get("value");

        public virtual bool HandlesDocNode(XObject docNode)
        {
            return (docNode is XElement) && ((XElement)docNode).Name.NamespaceName == XmlNs.FHIR;
        }

        public virtual FhirNavigationTree ConstructTreeNode(XObject docNode, FhirNavigationTree parent)
        {
            var docElement = (XElement)docNode;
            var newNodeName = docElement.Name.LocalName;       // ignore namespace, it's always FHIRNS
            bool hasValue = false;

            var value = tryGetValue(docElement, out hasValue);

            if (hasValue)
                return parent.AddLastChild(newNodeName, value);
            else
                return parent.AddLastChild(newNodeName);
        }

        public virtual IEnumerable<XObject> SelectChildren(XObject docNode, FhirNavigationTree treeNode)
        {
            var docElement = (XElement)docNode;

            if (docElement.HasAttributes)
            {
                var attributes = getFhirNonValueAttributes(docElement);
                foreach (var attribute in attributes)
                    yield return attribute;
            }

            foreach (var node in docElement.Nodes())
                yield return node;
        }

        public virtual IEnumerable<XObject> PostProcessChildren(IEnumerable<XObject> children, FhirNavigationTree treeNode)
        {
            return children;
        }


        private static string tryGetValue(XElement docNode, out bool hasValue)
        {
            var valueAttribute = docNode.Attribute(XVALUE);

            if (valueAttribute != null)
            {
                hasValue = true;
                return valueAttribute.Value;
            }
            else
            {
                hasValue = false;
                return null;
            }
        }


        private static IEnumerable<XAttribute> getFhirNonValueAttributes(XElement node)
        {
            foreach (var attr in node.Attributes())
            {
                // skip fhir "value" attribute
                if (attr.Name == XVALUE) continue;

                // skip xmlns declarations
                if (attr.IsNamespaceDeclaration) continue;

                // skip xmlns:xsi declaration
                if (attr.Name == XName.Get("{http://www.w3.org/2000/xmlns/}xsi") && !SerializationConfig.EnforceNoXsiAttributesOnRoot) continue;

                // skip schemaLocation
                if (attr.Name == XName.Get("{http://www.w3.org/2001/XMLSchema-instance}schemaLocation") && !SerializationConfig.EnforceNoXsiAttributesOnRoot) continue;

                if (attr.Name.NamespaceName == "")
                    yield return attr;
                else
                    throw Error.Format("Encountered unexpected attribute '{0}'.".FormatWith(attr.Name), XmlLineInfoWrapper.Wrap(attr));
            }
        }

    }
}