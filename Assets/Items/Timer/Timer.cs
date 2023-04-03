using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = System.Random;

namespace Variety
{
    public class Timer : Item
    {
        public int Cell { get; private set; }
        public TimerType FlavorType { get; private set; }
        public int NumPositions { get; private set; }

        private static readonly int[] Primes = new int[] { 2, 3, 5, 7 };

        public Timer(VarietyModule module, int cell, TimerType flavor, int A, int B) : base(module, new int[] { cell })
        {
            Cell = cell;
            FlavorType = flavor;
            _a = Primes[A];
            _b = Primes[B];
            NumPositions = _a * _b;
            SetState(-1, automatic: true);
        }

        public override void OnActivate()
        {
            _running = true;
            _timer = _prefab.StartCoroutine(RunTimer());
            _active = true;
        }

        public override int NumStates { get { return NumPositions; } }
        public override object Flavor { get { return FlavorType; } }

        private int _displayedTime, _a, _b;
        private bool _running, _active;
        private TimerPrefab _prefab;
        private Coroutine _timer;

        public override IEnumerable<ItemSelectable> SetUp(Random rnd)
        {
            _prefab = Object.Instantiate(Module.TimerTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 1));
            _prefab.transform.localScale = Vector3.one;
            _prefab.Selectable.OnInteract += Press;
            yield return new ItemSelectable(_prefab.Selectable, Cells[0]);
        }

        private bool Press()
        {
            _prefab.Selectable.AddInteractionPunch(.5f);
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _prefab.transform);

            if (!_active)
                return false;

            if (!_running)
            {
                _running = true;
                SetState(-1);
                _timer = _prefab.StartCoroutine(RunTimer());
            }
            else
            {
                _running = false;
                SetState(_displayedTime);
                _prefab.StopCoroutine(_timer);
            }
            return false;
        }

        private IEnumerator RunTimer()
        {
            float startTime = Time.time - _displayedTime;
            while (true)
            {
                int t = (int)(Time.time - startTime) % NumPositions;
                if (FlavorType == TimerType.Descending)
                    t = NumPositions - 1 - t;
                _displayedTime = t;
                _prefab.Text.text = FormatTime(t);
                yield return null;
            }
        }

        private string FormatTime(int t)
        {
            return (t / _b) + " " + (t % _b);
        }

        public override string DescribeSolutionState(int state)
        {
            return string.Format("set the {0} timer to {1}", FlavorType.ToString().ToLowerInvariant(), FormatTime(state));
        }

        public override string DescribeWhatUserDid()
        {
            if (_running)
                return string.Format("you left the {0} timer running", FlavorType.ToString().ToLowerInvariant());
            else
                return string.Format("you set the {0} timer to {1}", FlavorType.ToString().ToLowerInvariant(), FormatTime(State));
        }

        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            return string.Format("you should have set the {0} timer to {1} (you {2})", FlavorType.ToString().ToLowerInvariant(), FormatTime(desiredState), _running ? "left it running" : "set it to " + FormatTime(State));
        }

        public override string ToString() { return string.Format("{0} timer ({1}x{2})", FlavorType, _a, _b); }

        public override string TwitchHelpMessage { get { return "!{0} ascending timer 02 [stops the timer at that value] | !{0} ascending timer reset [restarts the timer running]"; } }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            if (Regex.IsMatch("", command) && !_running)
                _prefab.Selectable.OnInteract();

            var rx = new Regex(@"^\s*(?:" + (FlavorType == TimerType.Ascending ? "acsending|asc" : "descending|desc?") + @")\s+timer\s+([0-" + (char)('/' + _a) + "][0-" + (char)('/' + _b) + @"])\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var m = rx.Match(command);
            if (!m.Success)
                yield break;

            if (!_running)
            {
                _prefab.Selectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            var st = m.Groups[1].Value;
            var state = (st[0] - '0') * _b + st[1] - '0';
            yield return new WaitUntil(() => _displayedTime == state);
            _prefab.Selectable.OnInteract();
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            if (State == desiredState)
                yield break;

            if(!_running)
            {
                _prefab.Selectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitUntil(() => _displayedTime == desiredState);
            _prefab.Selectable.OnInteract();
        }
    }
}