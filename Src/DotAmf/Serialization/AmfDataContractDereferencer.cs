using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF data contract dereferencer.
    /// </summary>
    internal class AmfDataContractDereferencer : DataContractResolver
    {
        #region .ctor
        static AmfDataContractDereferencer()
        {
            AmfTypesTable = new Dictionary<string, Type>();

            foreach (var type in AmfTypes)
                AmfTypesTable[type.Name] = type;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfDataContractDereferencer()
        {
            XmlDictionary = new XmlDictionary();
        }
        #endregion

        #region Constants
        /// <summary>
        /// Known types.
        /// </summary>
        static public readonly Type[] KnownTypes = new[]
                                                       {
                                                           typeof(object[]),
                                                           typeof(Dictionary<string, object>)
                                                       };

        /// <summary>
        /// AMF types.
        /// </summary>
        static public readonly Type[] AmfTypes = new[]
                                                     {
                                                         typeof(AmfObject),
                                                         typeof(AmfTypeTraits),
                                                         typeof(AmfExternalizable)
                                                     };

        public static readonly Dictionary<string, Type> AmfTypesTable;
        #endregion

        #region Data
        /// <summary>
        /// XML dictionary for resolved types data.
        /// </summary>
        protected readonly XmlDictionary XmlDictionary;
        #endregion

        #region DataContractResolver implementation
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            //An AMF type
            if (typeNamespace == AmfSerializationContext.AmfNamespace)
            {
                Type type;

                if (IsAmfTypeName(typeName, out type))
                {
                    return type;
                }
            }

            //Look for a known type
            var result = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            return result;
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string alias;

            //An AMF type
            if (IsAmfType(type, out alias))
            {
                typeName = XmlDictionary.Add(alias);
                typeNamespace = XmlDictionary.Add(AmfSerializationContext.AmfNamespace);
                return true;
            }

            //Treat all arrays as untyped
            if (type.IsArray) type = typeof(object[]);

            //Convert enums to plain integers
            if (type.IsEnum) type = Enum.GetUnderlyingType(type);

            //Look for a known type
            var result = knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
            return result;
        }
        #endregion

        #region Dereferenced objects
        /// <summary>
        /// Create a dereferenced AMF object.
        /// </summary>
        /// <param name="source">Data contract object to dereference.</param>
        /// <returns>AMF object.</returns>
        public object CreateDereferencedObject(object source)
        {
            if (source == null) return null;

            var type = source.GetType();

            //An externizable object
            if (source is IExternalizable)
            {
                var result = new AmfExternalizable();
                var ext = (IExternalizable)source;

                result.TypeName = ext.TypeName;

                using (var stream = new MemoryStream())
                {
                    ext.WriteExternal(stream);
                    result.Data = stream.ToArray();
                }

                return result;
            }

            //A dynamic object
            if (type == typeof(Dictionary<string, object>))
            {
                var result = new AmfObject { Traits = new AmfTypeTraits() };
                var map = (Dictionary<string, object>)source;
                result.Traits.TypeName = AmfTypeTraits.BaseTypeAlias;
                result.Traits.ClassMembers = new string[0];
                result.Traits.IsDynamic = true;

                result.Properties = new Dictionary<string, object>(map);

                return result;
            }

            //A data contract
            if (!type.IsValueType)
            {
                var result = new AmfObject { Traits = new AmfTypeTraits() };
                var alias = DataContractHelper.GetContractAlias(type);
                var properties = DataContractHelper.GetContractProperties(source);

                result.Traits.TypeName = alias;
                result.Traits.ClassMembers = properties.Keys.ToArray();

                result.Properties = new Dictionary<string, object>(properties);

                return result;
            }

            return null;
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Check if a type name is an AMF type name.
        /// </summary>
        /// <param name="typeName">Type name to check.</param>
        /// <param name="type">AMF type.</param>
        static protected bool IsAmfTypeName(string typeName, out Type type)
        {
            type = AmfTypesTable.ContainsKey(typeName)
                ? AmfTypesTable[typeName]
                : null;

            return (type != null);
        }

        /// <summary>
        /// Check if a type is an AMF type.
        /// </summary> 
        /// <param name="type">Type to check.</param>
        /// <param name="typeName">AMF type name.</param>
        static protected bool IsAmfType(Type type, out string typeName)
        {
            typeName = AmfTypesTable
                .Where(pair => pair.Value == type)
                .Select(pair => pair.Key).FirstOrDefault();

            return (typeName != null);
        }

        /// <summary>
        /// Check if a type is an AMF type.
        /// </summary> 
        /// <param name="type">Type to check.</param>
        static protected bool IsAmfType(Type type)
        {
            string alias;
            return IsAmfType(type, out alias);
        }

        /// <summary>
        /// Check if type is serializable.
        /// </summary>
        static protected bool IsSerializableType(Type type)
        {
            return (type.IsValueType || type.IsArray || type == typeof(Dictionary<string, object>));
        }
        #endregion

        #region AMF dereferenced surrogate
        /// <summary>
        /// AMF dereferenced surrogate.
        /// </summary>
        sealed public class DereferencedSurrogate : IDataContractSurrogate
        {
            #region .ctor
            public DereferencedSurrogate(Func<object, object> objectFactoryMethod)
            {
                _objectFactoryMethod = objectFactoryMethod;
            }
            #endregion

            #region Data
            /// <summary>
            /// AMF dereferenced object factory method.
            /// </summary>
            private readonly Func<object, object> _objectFactoryMethod;
            #endregion

            #region IDataContractSurrogate implementation
            public Type GetDataContractType(Type type) { return type; }

            public object GetDeserializedObject(object obj, Type targetType) { return obj; }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                var type = obj.GetType();

                if (IsSerializableType(type) || IsAmfType(type))
                    return obj;

                return _objectFactoryMethod.Invoke(obj);
            }

            #region Not supported methods
            public object GetCustomDataToExport(Type clrType, Type dataContractType)
            {
                throw new NotSupportedException();
            }

            public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
            {
                throw new NotSupportedException();
            }

            public void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes)
            {
                throw new NotSupportedException();
            }

            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                throw new NotSupportedException();
            }

            public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
            {
                throw new NotSupportedException();
            }
            #endregion
            #endregion
        }
        #endregion
    }
}
