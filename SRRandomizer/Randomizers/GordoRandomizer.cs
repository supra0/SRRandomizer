using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRRandomizer
{
    class GordoRandomizer
    {
        #region Option Variables
        public int minFoodRequirement = 1;
        public int maxFoodRequirement = 100;
        #endregion

        public int GetRandomizedGordoFoodTarget()
        {
            return SRRandomizer.RuntimeRandNext(minFoodRequirement, maxFoodRequirement + 1);
        }
    }
}
