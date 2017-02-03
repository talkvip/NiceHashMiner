﻿using System;
using System.Collections.Generic;
using System.Text;
using NiceHashMiner.Enums;
using NiceHashMiner.Devices;
using NiceHashMiner.Configs;
using NiceHashMiner.Net20_backport;

namespace NiceHashMiner.Miners.Grouping {
    public class GroupMiner {
        public Miner Miner { get; protected set; }
        public string DevicesInfoString { get; private set; }
        public AlgorithmType AlgorithmType { get; private set; }
        // for now used only for dagger identification AMD or NVIDIA
        public DeviceType DeviceType { get; private set; }
        public double CurrentRate { get; set; }
        public string Key { get; private set; }

        // , string miningLocation, string btcAdress, string worker
        public GroupMiner(List<MiningPair> miningPairs, string key) {
            AlgorithmType = AlgorithmType.NONE;
            DevicesInfoString = "N/A";
            CurrentRate = 0;
            Key = key;
            if (miningPairs.Count > 0) {
                // sort pairs by device id
                miningPairs.Sort((a, b) => a.Device.ID - b.Device.ID);
                // init name scope
                {
                    List<string> deviceNames = new List<string>();
                    foreach (var pair in miningPairs) {
                        deviceNames.Add(pair.Device.NameCount);
                    }
                    DevicesInfoString = "{ " + StringHelper.Join(", ", deviceNames) + " }";
                }
                // init miner
                {
                    var mPair = miningPairs[0];
                    DeviceType = mPair.Device.DeviceType;
                    Miner = MinersManager.CreateMiner(mPair.Device.DeviceType,
                        MinerPaths.GetOptimizedMinerPath(mPair));
                    if(Miner != null) {
                        Miner.InitMiningSetup(new MiningSetup(miningPairs));
                        AlgorithmType = mPair.Algorithm.NiceHashID;
                    }
                }
            }
        }

        public void Stop() {
            if (Miner != null && Miner.IsRunning) {
                Miner.Stop(MinerStopType.SWITCH);
                // wait before going on
                System.Threading.Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);
            }
            CurrentRate = 0;
        }

        public void End() {
            if (Miner != null) {
                Miner.End();
            }
            CurrentRate = 0;
        }

        private string GetLocationURL(string miningLocation, NHMConectionType ConectionType) {
            string name = Globals.NiceHashData[AlgorithmType].name;
            int n_port = Globals.NiceHashData[AlgorithmType].port;
            int ssl_port = 30000 + n_port;

            // NHMConectionType.NONE
            string prefix = "";
            int port = n_port;
            if (NHMConectionType.LOCKED == Miner.ConectionType) {
                return miningLocation;
            }
            if (NHMConectionType.STRATUM_TCP == Miner.ConectionType) {
                prefix = "stratum+tcp://";
            }
            if (NHMConectionType.STRATUM_SSL == Miner.ConectionType) {
                prefix = "stratum+ssl://";
                port = ssl_port;
            }

            return prefix
                    + name
                    + "." + miningLocation
                    + ".nicehash.com:"
                    + port;
        }

        public void Start(string miningLocation, string btcAdress, string worker) {
            if(Miner.IsRunning) {
                return;
            }
            // Wait before new start
            System.Threading.Thread.Sleep(ConfigManager.GeneralConfig.MinerRestartDelayMS);

            string locationURL = GetLocationURL(miningLocation, Miner.ConectionType);
            Miner.Start(locationURL, btcAdress, worker);
        }
    }
}
