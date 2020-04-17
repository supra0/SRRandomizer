using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRRandomizer.Patches
{
    class Patch_ReplaceProduce
    {
        //hijack GameModel.RegisterResourceSpawner
        //SpawnResource.SpawnMetadata.prefab

        //alternatively, hijack SpawnResource.Spawn
        //which one gives me more flexibility?
    }
}
