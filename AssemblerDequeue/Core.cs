using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Fluent;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;

namespace AssemblerDequeue
{
    public class Core : TorchPluginBase
    {
        private TorchSessionManager _sessionManager;
        private static ConcurrentDictionary<MyAssembler, DateTime> _track = new ConcurrentDictionary<MyAssembler, DateTime>();
        private int _updateCounter;


        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _sessionManager = Torch.Managers.GetManager <TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
            AssemblerPatch.QueueAdded += AssemblerPatchOnQueueAdded;
        }

        private void AssemblerPatchOnQueueAdded(MyProductionBlock obj)
        {
            if (!(obj is MyAssembler assembler)) return;
            if (!_track.TryGetValue(assembler, out var time))
                _track[assembler]= DateTime.Now;
            if ((DateTime.Now - time).TotalSeconds < 10 || assembler.IsProducing) return;
            assembler.ClearQueue();
            var test =assembler.Queue;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    StartPlugin();
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newstate), newstate, null);
            }
        }

        private void StartPlugin()
        {
            var allGrids = MyEntities.GetEntities();

            foreach (var grid in allGrids.OfType<MyCubeGrid>())
            {
                var blocks = grid.CubeBlocks;
                foreach (var block in blocks.Select(x=>x.FatBlock))
                {
                    if (!(block is MyAssembler assembler) || assembler.IsQueueEmpty) continue;
                    _track[assembler] = DateTime.Now;
                }
            }
        }

        private void EmptyQueue()
        {
            if (_track.IsEmpty) return;
            foreach (var (block,time) in _track)
            {
                if ((DateTime.Now - time).TotalSeconds < 10)
                    continue;
                if (block.IsProducing)
                {
                    _track[block] = DateTime.Now; 
                    continue;
                }
                
                block.ClearQueue();
                _track.Remove(block);

            }
        }

        public override void Update()
        {
            base.Update();
            if (++_updateCounter % 100 == 0)
            {
                EmptyQueue();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _track.Clear();
        }
    }
}