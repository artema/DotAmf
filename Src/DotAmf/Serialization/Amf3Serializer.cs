using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF3 serializer.
    /// </summary>
    public class Amf3Serializer : Amf0Serializer
    {
        #region .ctor
        public Amf3Serializer(BinaryWriter writer, AmfVersion initialContext)
            : base(writer, initialContext)
        {
            _rollbackAction = () => Context = initialContext;

            _stringReferences = new List<string>();
            _traitReferences = new List<AmfTypeTraits>();
        }

        public Amf3Serializer(BinaryWriter writer)
            : this(writer, AmfVersion.Amf3)
        {
        }
        #endregion

        #region Constants
        /// <summary>
        /// A bit mask to truncate a value to <c>UInt29</c>.
        /// </summary>
        private const int UInt29Mask = 0x1FFFFFFF;

        /// <summary>
        /// The minimum value for an integer that will avoid
        /// promotion to an ActionScript's <c>Number</c> type.
        /// </summary>
        private const int MinInt29Value = -268435456;

        /// <summary>
        /// The maximum value for an integer that will avoid
        /// promotion to an ActionScript's <c>Number</c> type.
        /// </summary>
        private const int MaxInt29Value = 268435455;
        #endregion

        #region Data
        /// <summary>
        /// Strings references.
        /// </summary>
        private readonly List<string> _stringReferences;

        /// <summary>
        /// Object traits references.
        /// </summary>
        private readonly List<AmfTypeTraits> _traitReferences;
        #endregion

        #region References
        /// <summary>
        /// Save a string to a list of string references.
        /// </summary>
        /// <param name="value">String to save.</param>
        /// <returns><c>null</c> if the item was added to the reference list,
        /// or a position in reference list if the item has already been added.</returns>
        private int? SaveReference(string value)
        {
            var index = _stringReferences.BinarySearch(value);
            if (index != -1) return index;

            _stringReferences.Add(value);
            return null;
        }

        /// <summary>
        /// Save object traits to a list of traits references.
        /// </summary>
        /// <param name="value">Traits to save.</param>
        /// <returns><c>null</c> if the item was added to the reference list,
        /// or a position in reference list if the item has already been added.</returns>
        private int? SaveReference(AmfTypeTraits value)
        {
            var index = _traitReferences.BinarySearch(value);
            if (index != -1) return index;

            _traitReferences.Add(value);
            return null;
        }
        #endregion

        #region IAmfSerializer implementation
        override public void ClearReferences()
        {
            base.ClearReferences();

            _stringReferences.Clear();
            _traitReferences.Clear();
        }

        public override void WriteValue(object value)
        {
            //A null value
            if (value == null)
            {
                PerformWrite(WriteNull);
                return;
            }

            var type = value.GetType();

            //A nullable value
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type);

            //Check for types from base context
            if (Context != AmfVersion.Amf3 && IsBaseContextType(type))
            {
                base.WriteValue(value);
                return;
            }

            //A primitive value
            if (type.IsValueType || type.IsEnum || type == typeof(string))
            {
                PerformWrite(() => WritePrimitive(value));
                return;
            }
            
            //An array
            if (type.IsArray)
            {
                PerformWrite(() => WriteArrayValue(value));
                return;
            }

            //An AMF+ object
            if (type == typeof(AmfPlusObject))
            {
                PerformWrite(() => Write((AmfPlusObject)value));
                return;
            }

            //An XML document
            if (type == typeof(XmlDocument))
            {
                PerformWrite(() => Write((XmlDocument)value));
                return;
            }

            //An externizable object
            if(type.IsClass && type.GetInterfaces().Contains(typeof(IExternalizable)))
            {
                PerformWrite(() => Write((IExternalizable)value));
                return;
            }

            //Check for context violations
            if(IsBaseContextType(type))
                throw new SerializationException(
                    "Unable to serialize the type within current AMF version context: " 
                    + type.FullName);

            throw new SerializationException(
                "Unable to serialize the type: " 
                + type.FullName);
        }
        #endregion

        #region Special values
        /// <summary>
        /// Write an 29-bit unsigned integer.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private void WriteUInt29(int value)
        {
            //< 128:
            //0x00000000 - 0x0000007F
            if (value < 0x80)
            {
                Writer.Write((byte)value);                      //0xxxxxxx
            }
            //< 16,384:
            //0x00000080 - 0x00003FFF
            else if (value < 0x4000)
            {
                Writer.Write((byte)value >> 7 & 0x7F | 0x80);   //1xxxxxxx
                Writer.Write((byte)value & 0x7F);               //xxxxxxxx
            }
            //< 2,097,152:
            //0x00004000 - 0x001FFFFF
            else if (value < 0x200000)
            {
                Writer.Write((byte)value >> 14 & 0x7F | 0x80);  //1xxxxxxx
                Writer.Write((byte)value >> 7 & 0x7F | 0x80);   //1xxxxxxx
                Writer.Write((byte)value & 0x7F);               //xxxxxxxx
            }
            //0x00200000 - 0x3FFFFFFF
            else if (value < 0x40000000)
            {
                Writer.Write((byte)value >> 22 & 0x7F | 0x80);  //1xxxxxxx
                Writer.Write((byte)value >> 15 & 0x7F | 0x80);  //1xxxxxxx
                Writer.Write((byte)value >> 8 & 0x7F | 0x80);   //1xxxxxxx
                Writer.Write((byte)value & 0xFF);               //xxxxxxxx
            }
            //0x40000000 - 0xFFFFFFFF, out of range
            else
            {
                throw new IndexOutOfRangeException("Integer is out of range: " + value);
            }
        }

        /// <summary>
        /// Write a reference.
        /// </summary>
        private void WriteReference(int reference)
        {
            reference &= UInt29Mask; //Truncate value to UInt29

            //The first bit is a flag (representing whether an instance follows)
            //with value 0 to imply that this is not an instance but a reference.
            //The remaining 1 to 28 significant bits are used to encode a reference index.
            var flag = (reference << 1) | 0x1;

            WriteUInt29(flag);
        }

        /// <summary>
        /// Write a primitive value.
        /// </summary>
        private void WritePrimitive(object value)
        {
            var type = value.GetType();

            //A boolean value
            if (type == typeof(bool))
            {
                Write((bool)value);
                return;
            }

            //A string
            if (type == typeof(string))
            {
                Write((string)value);
                return;
            }

            //A numeric value
            bool isInteger;
            if (IsNumericType(type, out isInteger))
            {
                var intval = isInteger ? Convert.ToInt64(value) : 0;

                //Check if the value fits the Int29 span
                if (isInteger && intval >= MinInt29Value && intval <= MaxInt29Value)
                {
                    //It should be safe to cast it there
                    var integer = UInt29Mask & (int)intval; //Truncate the value

                    Write(Amf3TypeMarker.Integer);
                    WriteUInt29(integer); 
                }
                //Promote the value to a double
                else
                {
                    Write(Amf3TypeMarker.Double);
                    Writer.Write(Convert.ToDouble(value));
                }

                return;
            }

            //A date/time value
            if (type == typeof(DateTime))
            {
                Write((DateTime)value);
                return;
            }

            throw new SerializationException("Invalid type: " + type.FullName);
        }

        /// <summary>
        /// Write an array value.
        /// </summary>
        private void WriteArrayValue(object value)
        {
            var type = value.GetType();

            //A byte array
            if (typeof(byte[]) == type)
            {
                Write((byte[])value);
                return;
            }

            //A regular array
            Write(Amf3TypeMarker.Array);
            var reference = SaveReference(value);

            //Send array by reference
            if (reference.HasValue)
            {
                WriteReference(reference.Value);
                return;
            }

            var array = (object[]) value;

            //The first bit is a flag with value 1.
            //The remaining 1 to 28 significant bits 
            //are used to encode the count of the dense 
            //portion of the Array.
            var length = (array.Length << 1) | 0x1;
            WriteUInt29(length);

            WriteUtf8(string.Empty); //No associative values

            foreach (var item in array)
                WriteValue(item);
        }

        /// <summary>
        /// Write a <c>null</c>.
        /// </summary>
        private void WriteNull()
        {
            Write(Amf3TypeMarker.Null);
        }

        /// <summary>
        /// Write UTF-8 data.
        /// </summary>
        private void WriteUtf8(string data)
        {
            if (data == null) data = string.Empty;

            var decoded = data.ToCharArray();

            //The first bit is a flag with value 1.
            //The remaining 1 to 28 significant bits are used 
            //to encode the byte-length of the data
            var flag = (decoded.Length << 1) | 0x1;

            WriteUInt29(flag);
            Writer.Write(decoded);
        }
        #endregion

        #region Serialization methods
        /// <summary>
        /// Write an AMF3 type marker.
        /// </summary>
        public void Write(Amf3TypeMarker marker)
        {
            Writer.Write((byte)marker);
        }

        /// <summary>
        /// Write a boolean value.
        /// </summary>
        private void Write(bool value)
        {
            Write(value ? Amf3TypeMarker.True : Amf3TypeMarker.False);
        }

        /// <summary>
        /// Write a <c>DateTime</c>.
        /// </summary>
        private void Write(DateTime value)
        {
            Write(Amf3TypeMarker.Date);
            var reference = SaveReference(value);

            //Send date by reference
            if (reference.HasValue)
            {
                WriteReference(reference.Value);
                return;
            }

            //The first bit is a flag with value 1.
            //The remaining bits are not used.
            WriteUInt29(0 | 0x1);

            Writer.Write(ConvertToTimestamp(value));
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private void Write(string value)
        {
            Write(Amf3TypeMarker.String);
            var reference = SaveReference(value);

            if (reference.HasValue)
                WriteReference(reference.Value);
            else
                WriteUtf8(value);
        }

        /// <summary>
        /// Write a byte array.
        /// </summary>
        private void Write(byte[] bytes)
        {
            Write(Amf3TypeMarker.ByteArray);
            var reference = SaveReference(bytes);

            //Send byte array by reference
            if (reference.HasValue)
            {
                WriteReference(reference.Value);
                return;
            }

            //The first bit is a flag with value 1.
            //The remaining 1 to 28 significant bits are used 
            //to encode the byte-length of the data
            var flag = (bytes.Length << 1) | 0x1;
            WriteUInt29(flag);

            Writer.Write(bytes);
        }

        /// <summary>
        /// Write an XML.
        /// </summary>
        private void Write(XmlDocument value)
        {
            Write(Amf3TypeMarker.Xml);
            var reference = SaveReference(value);

            //Send XML by reference
            if (reference.HasValue)
            {
                WriteReference(reference.Value);
                return;
            }

            string xmlString;

            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                value.WriteTo(xmlTextWriter);
                xmlString = stringWriter.GetStringBuilder().ToString();
            }

            WriteUtf8(xmlString);
        }

        /// <summary>
        /// Write an AMF+ object.
        /// </summary>
        private void Write(AmfPlusObject obj)
        {
            Write(Amf3TypeMarker.Object);
            var objreference = SaveReference(obj);

            //Send object by reference
            if (objreference.HasValue)
            {
                WriteReference(objreference.Value);
                return;
            }

            var traitsreference = SaveReference(obj.Traits);

            //Send traits by reference
            if (traitsreference.HasValue)
            {
                var flag = traitsreference.Value & UInt29Mask; //Truncate value to UInt29

                //The first bit is a flag with value 1.
                //The second bit is a flag (representing whether a trait
                //reference follows) with value 0 to imply that this objects
                //traits are being sent by reference. The remaining 1 to 27 
                //significant bits are used to encode a trait reference index.
                flag = (flag << 2) | 0x2;
                WriteUInt29(flag);
            }
            //Send traits by value
            else
            {
                //The first bit is a flag with value 1. 
                //The second bit is a flag with value 1.
                //The third bit is a flag with value 0. 
                var flag = 0x3; //00000011

                //The fourth bit is a flag specifying whether the type is dynamic.
                //A value of 0 implies not dynamic, a value of 1 implies dynamic.
                if (obj.Traits.IsDynamic) flag |= 0x1 << 3; //0000*011

                //The remaining 1 to 25 significant bits are used to encode the number 
                //of sealed traits member names that follow after the class name.
                var count = obj.Traits.ClassMembers.Count();
                flag |= count << 4;

                WriteUInt29(flag);
                WriteUtf8(obj.Traits.TypeName);

                //Write member names
                foreach (var member in obj.Traits.ClassMembers)
                    WriteUtf8(member);
            }

            //Write member values
            foreach (var member in obj.Traits.ClassMembers)
                WriteValue(obj[member]);

            //Dynamic types may have a set of name value pairs
            //for dynamic members after the sealed member section.
            if(obj.Traits.IsDynamic)
            {
                var dynamicMembers = obj.Keys.Except(obj.Traits.ClassMembers);

                foreach (var member in dynamicMembers)
                {
                    WriteUtf8(member);
                    WriteValue(obj[member]);
                }
            }
        }

        /// <summary>
        /// Write an externizable object.
        /// </summary>
        private void Write(IExternalizable obj)
        {
            Write(Amf3TypeMarker.Object);

            //The first bit is a flag with value 1.
            //The second bit is a flag with value 1.
            //The third bit is a flag with value 1.
            //The remaining 1 to 26 significant bits are not significant
            //(the traits member count would always be 0).
            WriteUInt29(0x7); //00000111
            WriteUtf8(obj.TypeName);

            byte[] data;

            using(var stream = new MemoryStream())
            {
                obj.WriteExternal(stream);
                data = stream.ToArray();
            }

            Writer.Write(data);
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Check if type is a base context type.
        /// </summary>
        static private bool IsBaseContextType(Type type)
        {
            return (type == typeof (AmfObject) || type == typeof (TypedAmfObject));
        }

        /// <summary>
        /// Perform an AMF+ write action.
        /// </summary>
        private void PerformWrite(Action writeAction)
        {
            //Perform a context switch
            var rollback = SwitchContext();

            try
            {
                writeAction.Invoke();
            }
            finally
            {
                rollback.Invoke(); //Switch context back
            }
        }

        /// <summary>
        /// Mock context rollback action.
        /// </summary>
        private readonly Action _mockRollbackAction = delegate { };

        /// <summary>
        /// Context rollback action.
        /// </summary>
        private readonly Action _rollbackAction;

        /// <summary>
        /// Switch to new AMF+ context.
        /// </summary>
        /// <returns>An action that can be executed 
        /// to rollback the context to its previous state.</returns>
        private Action SwitchContext()
        {
            if (Context == AmfVersion.Amf3) 
                return _mockRollbackAction;

            Context = AmfVersion.Amf3;
            return _rollbackAction;
        }
        #endregion
    }
}
