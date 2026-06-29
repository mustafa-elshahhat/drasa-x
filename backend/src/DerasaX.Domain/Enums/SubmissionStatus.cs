using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Enums
{
    public enum SubmissionStatus
    {
        Submitted=1,
        Graded=2,
        Late=3,
        /// <summary>
        /// An attempt that has been started by the student but not yet submitted.
        /// Stored as a string ("InProgress") via the global enum-to-string conversion,
        /// so adding this member is migration-safe (no schema/model change).
        /// </summary>
        InProgress=4
    }
}
