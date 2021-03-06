﻿using LuckParser.Controllers;
using LuckParser.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace LuckParser.Models.ParseModels
{
    public class Target : AbstractMasterActor
    {
        // Constructors
        public Target(AgentItem agent) : base(agent)
        {
        }

        public int Health { get; set; } = -1;
        public List<Point> HealthOverTime { get; } = new List<Point>();

        /*public void AddCustomCastLog(long time, long skillID, int expDur, ParseEnum.Activation startActivation, int actDur, ParseEnum.Activation endActivation, ParsedLog log)
        {
            if (CastLogs.Count == 0)
            {
                GetCastLogs(log, 0, log.FightData.FightEnd);
            }
            CastLogs.Add(new CastLog(time, skillID, expDur, startActivation, actDur, endActivation, Agent, InstID));
        }*/

        // Private Methods

        protected override void SetAdditionalCombatReplayData(ParsedLog log)
        {
            CombatReplay.Icon = GeneralHelper.GetNPCIcon(ID);
            log.FightData.Logic.ComputeAdditionalTargetData(this, log);
            List<Point3D> facings = CombatReplay.Rotations;
            if (facings.Any())
            {
                CombatReplay.Actors.Add(new FacingActor(((int)CombatReplay.TimeOffsets.start, (int)CombatReplay.TimeOffsets.end), new AgentConnector(this), facings));
            }
        }

        public void OverrideName(string name)
        {
            Character = name;
        }

        public List<double[]> Get1SHealthGraph(ParsedLog log, List<PhaseData> phases)
        {
            List<double[]> res = new List<double[]>();
            // fill the graph, full precision
            List<double> listFull = new List<double>();
            for (int i = 0; i <= phases[0].GetDuration(); i++)
            {
                listFull.Add(100.0);
            }
            int totalTime = 0;
            double curHealth = 100.0;
            foreach (Point p in HealthOverTime)
            {
                int time = p.X;
                if (time < 0)
                {
                    continue;
                }
                if (time > phases[0].GetDuration())
                {
                    break;
                }
                for (; totalTime < time; totalTime++)
                {
                    listFull[totalTime] = curHealth;
                }
                curHealth = p.Y / 100.0;
                listFull[time] = curHealth;
            }
            // fill
            for (; totalTime <= phases[0].GetDuration(); totalTime++)
            {
                listFull[totalTime] = curHealth;
            }
            foreach (PhaseData phase in phases)
            {
                int seconds = (int)phase.GetDuration("s");
                bool needsLastPoint = seconds * 1000 != phase.GetDuration();
                double[] hps = new double[seconds + (needsLastPoint ? +2 : 1)];
                int time = (int)phase.Start;
                int i = 0;
                for (i = 0; i <= seconds; i++)
                {
                    hps[i] = listFull[time];
                    time += 1000;
                }
                if (needsLastPoint)
                {
                    hps[i] = listFull[(int)phase.End];
                }
                res.Add(hps);
            }
            return res;
        }
        

        //
        private class TargetSerializable : AbstractMasterActorSerializable
        {
            public long Start { get; set; }
            public long End { get; set; }
        }

        public override AbstractMasterActorSerializable GetCombatReplayJSON(CombatReplayMap map)
        {
            TargetSerializable aux = new TargetSerializable
            {
                Img = CombatReplay.Icon,
                Type = "Target",
                ID = GetCombatReplayID(),
                Start = CombatReplay.TimeOffsets.start,
                End = CombatReplay.TimeOffsets.end,
                Positions = new double[2 * CombatReplay.Positions.Count]
            };
            int i = 0;
            foreach (Point3D pos in CombatReplay.Positions)
            {
                (double x, double y) = map.GetMapCoord(pos.X, pos.Y);
                aux.Positions[i++] = x;
                aux.Positions[i++] = y;
            }
            return aux;
        }

        /*protected override void setHealingLogs(ParsedLog log)
        {
            // nothing to do
        }

        protected override void setHealingReceivedLogs(ParsedLog log)
        {
            // nothing to do
        }*/
    }
}