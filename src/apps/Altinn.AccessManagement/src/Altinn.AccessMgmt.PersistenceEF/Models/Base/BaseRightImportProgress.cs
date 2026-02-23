using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base
{
    public class BaseRightImportProgress
    {
        private Guid _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRightImportProgress"/> class.
        /// </summary>
        public BaseRightImportProgress()
        {
            Id = Guid.CreateVersion7();
        }

        /// <summary>
        /// Identity
        /// </summary>
        public Guid Id
        {
            get => _id;
            set
            {
                if (!value.IsVersion7Uuid())
                {
                    throw new ArgumentException("Id must be a version 7 UUID", nameof(value));
                }

                _id = value;
            }
        }
    }
}
