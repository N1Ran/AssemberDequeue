using System;
using System.Reflection;
using NLog;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;

namespace AssemblerDequeue
{
    [PatchShim]
    public static class AssemblerPatch
    {

        private static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyProductionBlock).GetMethod("OnAddQueueItemRequest", BindingFlags.NonPublic | BindingFlags.Instance)).Prefixes
                .Add(typeof(AssemblerPatch).GetMethod(nameof(OnAddItem), BindingFlags.Static | BindingFlags.NonPublic));
        }

        public static event Action<MyProductionBlock> QueueAdded;

        private static bool OnAddItem(MyProductionBlock __instance)
        {
            QueueAdded?.Invoke(__instance);
            return true;
        }
    }
}