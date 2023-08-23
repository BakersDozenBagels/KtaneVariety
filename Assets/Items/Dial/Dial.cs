using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

namespace Variety
{
    public class Dial : Item
    {
        public override string TwitchHelpMessage { get { return "!{0} red dial 0 [turn dial that many times] !{0} red dial cycle [turn the dial slowly]"; } }

        public override void SetColorblind(bool on)
        {
            _dial.GetComponentInChildren<TextMesh>(true).gameObject.SetActive(on);
        }

        public bool[] RealTicks { get; private set; }
        public int[] States { get; private set; }
        public int Rotation { get; private set; }
        public DialColor Color { get; private set; }

        private Coroutine _turning;
        private KMSelectable _dial;

        public Dial(VarietyModule module, int topLeftCell, DialColor color, int n, System.Random rnd)
            : base(module, CellRect(topLeftCell, 2, 2))
        {
            RealTicks = Enumerable.Range(0, 8).Select(i => i < n).OrderBy(_ => rnd.NextDouble()).ToArray();
            Rotation = rnd.Next(0, 8);
            States = Enumerable.Range(1, 8).Select(i => RealTicks[i - 1] ? RealTicks.Take(i).Count(b => b) - 1 : -1).ToArray();
            Color = color;

            SetState(States[Rotation], automatic: true);
        }

        private void SetPosition(int pos)
        {
            SetState(States[pos]);
            if(_turning != null)
                Module.StopCoroutine(_turning);
            _turning = Module.StartCoroutine(turnTo(pos));
        }

        private IEnumerator turnTo(int pos)
        {
            var oldRot = _dial.transform.localRotation;
            var newRot = Quaternion.Euler(0f, 45f * pos, 0f);
            var duration = .1f;
            var elapsed = 0f;
            while(elapsed < duration)
            {
                _dial.transform.localRotation = Quaternion.Slerp(oldRot, newRot, Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _dial.transform.localRotation = newRot;
            _turning = null;
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = Object.Instantiate(Module.DialTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .01501f, GetYOfCellRect(Cells[0], 2));
            prefab.transform.localRotation = Quaternion.Euler(0f, 90f * (int)Color, 0f);
            prefab.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            _dial = prefab.Dial;
            _dial.GetComponentInChildren<TextMesh>(true).text = Color.ToString().Substring(0, 1);
            _dial.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial = prefab.Materials[(int)Color];
            _dial.transform.localRotation = Quaternion.Euler(0f, 45f * Rotation, 0f);
            _dial.OnInteract = delegate
            {
                _dial.AddInteractionPunch(.25f);
                Rotation += 1;
                Rotation %= 8;
                Module.Audio.PlayGameSoundAtTransform(RealTicks[Rotation] ? KMSoundOverride.SoundEffect.BigButtonPress : KMSoundOverride.SoundEffect.ButtonPress, _dial.transform);
#if UNITY_EDITOR
                Debug.Log(RealTicks[Rotation] ? "Click" : "No click");
#endif
                SetPosition(Rotation);
                return false;
            };
            yield return new ItemSelectable(_dial, Cells[0]);
        }

        public override string ToString() { return string.Format("{0} dial ({1} clicks)", ColorName, RealTicks.Count(b => b)); }
        public override int NumStates { get { return RealTicks.Count(b => b); } }
        public override object Flavor { get { return Color; } }
        private static readonly string[] Ord = new string[] { "0th", "1st", "2nd", "3rd", "4th", "5th", "6th", "7th" };
        private string ColorName { get { return new string[] { "red", "green", "blue", "yellow" }[(int)Color]; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the {0} dial to the {1} clicking spot", ColorName, Ord[state]); }
        public override string DescribeWhatUserDid() { return string.Format("you turned the {0} dial", ColorName); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have set the {0} dial to the {1} clicking spot (instead of {3}{2})", ColorName, Ord[desiredState], State != -1 ? Ord[State] : "a non-clicking spot", State != -1 ? "the " : ""); }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*" + ColorName + @"\s+dial\s+cycle((?:fast)?)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if(m.Success)
                return TwitchCycle(m.Groups[1].Value.Length != 0).GetEnumerator();
            m = Regex.Match(command, @"^\s*" + ColorName + @"\s+dial\s+(\d)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            int val;
            if(m.Success && int.TryParse(m.Groups[1].Value, out val) && val > 0 && val < 8)
                return TwitchPress(val).GetEnumerator();
            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return TwitchPress((System.Array.IndexOf(States, desiredState) - Rotation + 8) % 8);
        }

        private IEnumerable<object> TwitchCycle(bool fast)
        {
            for(int i = 0; i < 8; i++)
            {
                _dial.OnInteract();
                yield return new WaitForSeconds(fast ? 0.3f : .9f);
            }
        }

        private IEnumerable<object> TwitchPress(int val)
        {
            for(int i = 0; i < val; i++)
            {
                _dial.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}