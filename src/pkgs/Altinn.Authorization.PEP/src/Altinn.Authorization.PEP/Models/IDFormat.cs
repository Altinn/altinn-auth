namespace Altinn.Common.PEP.Models
{
    /// <summary>
    ///  IDFormat is used to communicate the format of a suspected string of either OrgNr or SSN -type.
    /// </summary>
    /// <remarks>
    ///  Author: Ole Hansen
    ///  Date: 29/04/2009
    /// </remarks>
    public enum IDFormat : int
    {
        /// <summary>
        /// IDFormat is unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// IDFormat is SSN
        /// </summary>
        SSN = 1,

        /// <summary>
        /// IDFormat is OrgNr
        /// </summary>
        OrgNr = 2,

        /// <summary>
        /// IDFormat is Self Identified User
        /// </summary>
        UserName = 3,
    }
}
