using System;
using static Nextended.Core.DeepClone.Extensions;

namespace Nextended.Core.DeepClone
{
    /// <summary>
    /// FastDeepClonerSettings
    /// </summary>
    public class ClonerSettings
    {
        /// <summary>
        /// Field type
        /// </summary>
        public FieldType FieldType { get; set; }

        /// <summary>
        ///  CloneDeep Level
        /// </summary>
        public CloneLevel CloneLevel { get; set; }

        /// <summary>
        /// override Activator CreateInstance and use your own method
        /// </summary>
        public CreateInstance OnCreateInstance { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClonerSettings()
        {
            OnCreateInstance = new CreateInstance((Type type) =>
            {
                return type.Creator();
            });
        }

    }
}
