using System.Collections.Generic;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF3 class aliases utility.
    /// </summary>
    internal static class ClassAliasUtil
    {
        #region Aliases
        /// <summary>
        /// Core Flex classes' aliases.
        /// </summary>
        static public readonly Dictionary<string, string> CoreFlexAliases = new Dictionary<string, string>
        {
            {"DSK", "flex.messaging.messages.AcknowledgeMessageExt"},
            {"DSA", "flex.messaging.messages.AsyncMessageExt"},
            {"DSC", "flex.messaging.messages.CommandMessageExt"}      
        };
        #endregion
    }
}
