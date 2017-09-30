namespace CODE.Framework.Core.Newtonsoft
{
    /// <summary>
    ///     Indicates the method that will be used during deserialization for locating and loading assemblies.
    /// </summary>
    public enum TypeNameAssemblyFormatHandling
    {
        /// <summary>
        ///     In simple mode, the assembly used during deserialization need not match exactly the assembly used during
        ///     serialization. Specifically, the version numbers need not match as the <c>LoadWithPartialName</c> method of the
        ///     <see cref="System.Reflection.Assembly" /> class is used to load the assembly.
        /// </summary>
        Simple = 0,

        /// <summary>
        ///     In full mode, the assembly used during deserialization must match exactly the assembly used during serialization.
        ///     The <c>Load</c> method of the <see cref="System.Reflection.Assembly" /> class is used to load the assembly.
        /// </summary>
        Full = 1
    }
}