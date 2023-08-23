using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Bulb : Item
    {
        public override string TwitchHelpMessage { get { return "!{0} red bulb ..- [transmit ..- on the red bulb] | !{0} red bulb reset [show flashing code again]"; } }

        public override void SetColorblind(bool on)
        {
            ColorblindText.gameObject.SetActive(on);
            ColorblindText.text = _colorblindNames[(int)Color];
        }
        private TextMesh _colorblindText;
        private TextMesh ColorblindText
        {
            get
            {
                if (_colorblindText == null)
                    _colorblindText = _prefab.GetComponentInChildren<TextMesh>(true);
                return _colorblindText;
            }
        }

        public int TopLeftCell { get; private set; }
        public BulbColor Color { get; private set; }
        public int N { get; private set; }
        private string _inputs;

        public Bulb(VarietyModule module, int topLeftCell, BulbColor color, int n)
            : base(module, CellRect(topLeftCell, 2, 2))
        {
            TopLeftCell = topLeftCell;
            Color = color;
            N = n;
            SetState(-1, automatic: true);
        }

        private BulbState _cyclingState = BulbState.Flashing;
        private KMSelectable _button;
        private BulbPrefab _prefab;
        private Coroutine _morseCycle = null;

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.BulbTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 2));
            float r = UnityEngine.Random.Range(0f, 360f);
            _prefab.transform.localRotation = Quaternion.Euler(0f, r, 0f);
            ColorblindText.transform.localRotation = Quaternion.Euler(90f, -r, 0f);
            _prefab.transform.localScale = new Vector3(2, 2, 2);

            float t = Time.time + UnityEngine.Random.Range(0f, 2f);
            Module.StartCoroutine(Delay(() => Time.time < t, () => false, () => _morseCycle = Module.StartCoroutine(CycleMorse(Morse(N.ToString())))));
            _button = _prefab.Selectable;

            float lastTime = -1f;
            bool held = false;
            List<float> delays = new List<float>();
            _button.OnInteract = delegate
            {
                if (_morseCycle != null)
                    Module.StopCoroutine(_morseCycle);
                switch (_cyclingState)
                {
                    case BulbState.Flashing:
                        _cyclingState = BulbState.Inputting;
                        lastTime = Time.time;
                        held = true;
                        _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int)Color + 3];
                        break;
                    case BulbState.Inputting:
                        delays.Add(Time.time - lastTime);
                        lastTime = Time.time;
                        held = true;
                        _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int)Color + 3];
                        break;
                    case BulbState.Repeating:
                        _cyclingState = BulbState.Flashing;
                        SetState(-1);
                        break;
                }
                if (_cyclingState == BulbState.Flashing)
                    _morseCycle = Module.StartCoroutine(CycleMorse(Morse(N.ToString())));
                return false;
            };
            _button.OnInteractEnded = delegate
            {
                if (_cyclingState != BulbState.Inputting)
                    return;
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int)Color];
                delays.Add(Time.time - lastTime);
                lastTime = Time.time;
                held = false;
                Module.StartCoroutine(Delay(() => Time.time - lastTime < 2f, () => held, () => { ProcessInput(delays); delays.Clear(); }));
            };
            yield return new ItemSelectable(_button, Cells[0]);
        }

        private void ProcessInput(List<float> delays)
        {
            char letter;
            if (delays.Count % 2 == 0 || delays.Count <= 0)
                throw new ArgumentException("This should be unreachable. " + delays.Join(", "));
            if (delays.Count == 1)
            {
                letter = delays[0] < 0.5 ? 'E' : 'T';
                _inputs = delays[0] < 0.5 ? "." : "-";
            }
            else
            {
                float total = 0f;
                for (int i = delays.Count - 2; i >= 0; i -= 2)
                    total += delays[i];
                total /= (delays.Count - 1) / 2;
                _inputs = "";
                for (int i = 0; i < delays.Count; i += 2)
                    _inputs += delays[i] > total ? "-" : ".";
                letter = Alphabet.FirstOrDefault(c => Morse(c) == _inputs);
            }

            int state = Alphabet.IndexOf(letter);
            if (state >= N)
                state = -1;
            SetState(state);
            _cyclingState = BulbState.Repeating;
            _morseCycle = Module.StartCoroutine(CycleMorse(_inputs));
        }

        IEnumerator Delay(Func<bool> check, Func<bool> stop, Action callback)
        {
            while (check())
            {
                if (stop())
                    yield break;
                yield return null;
            }
            if (stop())
                yield break;
            callback();
        }

        private static string Morse(string v)
        {
            return v.ToUpperInvariant().Select(Morse).Join(" ");
        }

        private static string Morse(char v)
        {
            switch (v)
            {
                case 'A':
                    return ".-";
                case 'B':
                    return "-...";
                case 'C':
                    return "-.-.";
                case 'D':
                    return "-..";
                case 'E':
                    return ".";
                case 'F':
                    return "..-.";
                case 'G':
                    return "--.";
                case 'H':
                    return "....";
                case 'I':
                    return "..";
                case 'J':
                    return ".---";
                case 'K':
                    return "-.-";
                case 'L':
                    return ".-..";
                case 'M':
                    return "--";
                case 'N':
                    return "-.";
                case 'O':
                    return "---";
                case 'P':
                    return ".--.";
                case 'Q':
                    return "--.-";
                case 'R':
                    return ".-.";
                case 'S':
                    return "...";
                case 'T':
                    return "-";
                case 'U':
                    return "..-";
                case 'V':
                    return "...-";
                case 'W':
                    return ".--";
                case 'X':
                    return "-..-";
                case 'Y':
                    return "-.--";
                case 'Z':
                    return "--..";
                case '0':
                    return "-----";
                case '1':
                    return ".----";
                case '2':
                    return "..---";
                case '3':
                    return "...--";
                case '4':
                    return "....-";
                case '5':
                    return ".....";
                case '6':
                    return "-....";
                case '7':
                    return "--...";
                case '8':
                    return "---..";
                case '9':
                    return "----.";
            }
            return string.Empty;
        }

        private IEnumerator CycleMorse(string morse)
        {
            if (morse.Length == 0)
            {
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int)Color];
                yield break;
            }
            var i = 0;
            var n = morse.Length;
            bool on = true;
            while (true)
            {
                on = morse[i] != ' ';
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int)Color + (on ? 3 : 0)];
                yield return new WaitForSeconds(morse[i] == '.' ? 0.3f : 0.9f);
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int)Color];
                yield return new WaitForSeconds(.3f);
                i = (i + 1) % n;
                if (i == 0)
                    yield return new WaitForSeconds(1.2f);
            }
        }

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string[] _colorNames = { "red", "yellow", "white" };
        private static readonly string[] _colorblindNames = { "R", "Y", "W" };

        public override string ToString() { return string.Format("{1} bulb flashing {0}", Morse(N.ToString()), _colorNames[(int)Color]); }
        public override int NumStates { get { return N; } }
        public override object Flavor { get { return Color; } }
        public override string DescribeSolutionState(int state) { return string.Format("tranmit {0} on the {1} bulb", Morse(Alphabet[state]), _colorNames[(int)Color]); }
        public override string DescribeWhatUserDid() { return string.Format("you transmitted {0} on the {1} bulb", _inputs, _colorNames[(int)Color]); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have transmitted {0} on the {1} bulb ({2})", Morse(Alphabet[desiredState]), _colorNames[(int)Color], State == -1 ? "you left it cycling" : "instead of " + _inputs); }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, string.Format(@"^\s*{0}\s+bulb\s+reset\s*$", _colorNames[(int)Color]), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchReset().GetEnumerator();

            m = Regex.Match(command, @"^\s*" + _colorNames[(int)Color] + @"\s+bulb\s+([.-]{1,4})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchSet(m.Groups[1].Value).GetEnumerator();

            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            foreach(object e in TwitchSet(Morse(Alphabet[desiredState]), true))
                yield return e;
            while(_cyclingState == BulbState.Inputting)
                yield return true;
        }

        private IEnumerable<object> TwitchSet(string mc, bool forceSolve = false)
        {
            if (_cyclingState == BulbState.Repeating)
            {
                _button.OnInteract();
                yield return new WaitForSeconds(.1f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }

            if (_cyclingState == BulbState.Inputting)
            {
                foreach (object e in WaitUntilWith(() => _cyclingState != BulbState.Inputting, forceSolve ? (object)true : "trycancel"))
                    yield return e;
                yield return new WaitForSeconds(.1f);
                _button.OnInteract();
                yield return new WaitForSeconds(.1f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }

            foreach (char c in mc)
            {
                _button.OnInteract();
                yield return new WaitForSeconds(c == '.' ? 0.3f : 0.9f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.6f);
            }
        }

        private IEnumerable<object> TwitchReset()
        {
            if (_cyclingState == BulbState.Flashing)
                yield break;
            if (_cyclingState == BulbState.Inputting)
            {
                foreach (object e in WaitUntilWith(() => _cyclingState != BulbState.Inputting, true))
                    yield return e;
                yield return new WaitForSeconds(.1f);
            }
            _button.OnInteract();
            yield return new WaitForSeconds(.1f);
            _button.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }

        public enum BulbColor
        {
            Red,
            Yellow,
            White
        }

        private enum BulbState
        {
            Flashing,
            Inputting,
            Repeating
        }

        private IEnumerable<object> WaitUntilWith(Func<bool> condition, object item)
        {
            while (!condition())
                yield return item;
        }
    }
}