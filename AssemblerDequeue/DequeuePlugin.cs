using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;

namespace AssemblerDequeue
{
    public class DequeuePlugin : TorchPluginBase, IWpfPlugin
    {
        private TorchSessionManager _sessionManager;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private static ConcurrentDictionary<MyAssembler, DateTime> _track = new ConcurrentDictionary<MyAssembler, DateTime>();
        private int _updateCounter;
        
        public static DequeuePlugin Instance { get; private set; }        
        
        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));
        private Persistent<DequeueConfig> _config;
        public DequeueConfig Config => _config?.Data;

        public void Save() => _config.Save();
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            
            var configFile = Path.Combine(StoragePath, "AssemblerDequeue.cfg");

            try 
            {

                _config = Persistent<DequeueConfig>.Load(configFile);

            }
            catch (Exception e) 
            {
                Log.Warn(e);
            }

            if (_config?.Data == null) {

                Log.Info("Created Default Config, because none was found!");

                _config = new Persistent<DequeueConfig>(configFile, new DequeueConfig());
                _config.Save();
            }
            
            _sessionManager = Torch.Managers.GetManager <TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;

        }

        private void AssemblerPatchOnQueueAdded(MyProductionBlock obj)
        {
            if (!Config.Enabled || !(obj is MyAssembler assembler)) return;
            if (!_track.TryGetValue(assembler, out _))
                _track[assembler]= DateTime.Now;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    AssemblerPatch.QueueAdded += AssemblerPatchOnQueueAdded;
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
            if (!Config.Enabled) return;
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
            if (!Config.Enabled || _track.IsEmpty) return;
            foreach (var (block,time) in _track)
            {
                if ((DateTime.Now - time).TotalSeconds < Config.DelayInSeconds)
                    continue;
                if (block.IsProducing)
                {
                    _track[block] = DateTime.Now; 
                    continue;
                }

                if (block.IsQueueEmpty)
                {
                    _track.Remove(block);
                    continue;
                }
                var item = block.GetQueueItem(0);
                block.RemoveQueueItemRequest(0,item.Amount);
                _track[block] = DateTime.Now;

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