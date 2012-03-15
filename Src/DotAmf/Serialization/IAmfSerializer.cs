namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF serializer.
    /// </summary>
    public interface IAmfSerializer
    {
        /// <summary>
        /// Write a value.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of bytes written.</returns>
        int WriteValue(object value);

        /// <summary>
        /// Clear stored references.
        /// </summary>
        void ClearReferences();
    }
}
