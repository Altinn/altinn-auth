using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models
{
    public class RightImportProgress : BaseRightImportProgress
    {
        /// <summary>
        /// The associated delegationChangeId (app/resource/instance)
        /// </summary>
        public long DelegationChangeId { get; set; }

        /// <summary>
        /// The origin of the change (app/resource/instance)
        /// </summary>
        public string OriginType { get; set; }
    }
}
