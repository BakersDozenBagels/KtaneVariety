﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Variety
{
    public class Key : Item
    {
        public Key(VarietyModule module, int cell) : base(module, new[] { cell, cell + 1, cell + W, cell + W + 1 })
        {
            TopLeftCell = cell;
            Turned = false;
            State = -1;
        }

        public int TopLeftCell { get; private set; }
        public bool Turned { get; private set; }

        private Transform _core;
        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.KeyTemplate, Module.transform);
            _core = prefab.Core;
            prefab.transform.localPosition = new Vector3(GetX(TopLeftCell) + VarietyModule.CellWidth / 2, .015f, GetY(TopLeftCell) - VarietyModule.CellHeight / 2);
            yield return new ItemSelectable(prefab.Key, Cells[0]);
            prefab.Key.OnInteract = TurnKey;
        }

        private bool TurnKey()
        {
            Turned = !Turned;
            State = Turned ? (int) Module.Bomb.GetTime() % 10 : -1;
            Module.StartCoroutine(KeyTurnAnimation(Turned));
            return false;
        }

        private IEnumerator KeyTurnAnimation(bool forwards)
        {
            var elapsed = 0f;
            var duration = .16f;

            while (elapsed < duration)
            {
                _core.transform.localEulerAngles = new Vector3(0, Mathf.Lerp(forwards ? 0 : 60, forwards ? 60 : 0, elapsed / duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _core.transform.localEulerAngles = new Vector3(0, forwards ? 60 : 0, 0);
        }

        public override string ToString()
        {
            return string.Format("Key at {0}", coords(TopLeftCell));
        }

        public override int NumStates { get { return 10; } }
        public override object Flavor { get { return "Key"; } }
        public override string DescribeSolutionState(int state) { return string.Format("turn the key when the last digit of the timer is {0}", state); }
        public override string DescribeWhatUserDid() { return "you turned the key"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have turned the key when the last digit on the timer was {0} ({1})", desiredState, State == -1 ? "you left it unturned" : string.Format("instead of {0}", State)); }
    }
}