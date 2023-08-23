using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class CoordinateGrid : Item
    {
        public override string TwitchHelpMessage { get { return "!{0} 3x3 grid 6 [selects the 6th coordinate in reading order in the 3×3 coordinate grid]"; } }

        public CoordinateGrid(VarietyModule module, int x, int y, int width, int height, int func) : base(module, Enumerable.Range(0, width * height).Select(ix => x + ix % width + W * (y + ix / width)).ToArray())
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            SetState(-25, automatic: true);
            Function = func;

            int count = -1;
            States = new int[width * height];
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    States[dx + dy * width] = Functions[Function][height - 1 - dy, dx] ? ++count : -(1 + dx + dy * width);
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Function { get; private set; }
        public override int NumStates { get { return States.Count(s => s >= 0); } }
        public int[] States { get; private set; }
        public int Selection { get; private set; }

        private static readonly bool[][,] Functions = new bool[][,]
        {
            new bool[,] { { true, false, false, false }, { false, true, false, true }, { false, false, true, false }, { false, true, false, true } },
            new bool[,] { { false, true, false, false }, { true, false, true, false }, { false, true, false, false }, { false, false, false, false} },
            new bool[,] { { true, false, false, false }, { false, true, false, false }, { true, false, false, false }, { false, true, false, false } },
            new bool[,] { { true, false, false, true }, { false, true, false, false }, { false, false, true, false }, { false, false, false, false } },
            new bool[,] { { false, false, false, false }, { true, true, true, true }, { false, false, false, false }, { false, false, false, false } },
            new bool[,] { { false, true, false, true}, { false, true, false, true }, { false, true, false, true }, { false, true, false, true } }
        };

        private KMSelectable[] _buttons;

        private Vector3 Pos(int cell)
        {
            return new Vector3((cell % Width) - (Width - 1) * .5f, .003f, (Height - 1) * .5f - (cell / Width));
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = UnityEngine.Object.Instantiate(Module.CoordinateGridTemplate, Module.transform);

            var cx = -VarietyModule.Width / 2 + (X + Width * .5f) * VarietyModule.CellWidth;
            var cy = VarietyModule.Height / 2 - (Y + Height * .5f) * VarietyModule.CellHeight + VarietyModule.YOffset;
            prefab.transform.localPosition = new Vector3(cx, .01502f, cy);
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = new Vector3(VarietyModule.CellWidth * .75f, VarietyModule.CellWidth * .75f, VarietyModule.CellWidth * .75f);

            _buttons = new KMSelectable[Width * Height];
            for (var dx = 0; dx < Width; dx++)
                for (var dy = 0; dy < Height; dy++)
                {
                    var dot = dx == 0 && dy == 0 ? prefab.Button : UnityEngine.Object.Instantiate(prefab.Button, prefab.transform);
                    dot.transform.localPosition = Pos(dx + Width * dy);
                    dot.transform.localEulerAngles = new Vector3(90, 0, 0);
                    dot.transform.localScale = new Vector3(.3f, .3f, .3f);
                    dot.gameObject.SetActive(dx + Width * dy != State);
                    _buttons[dx + Width * dy] = dot;
                    yield return new ItemSelectable(dot, X + dx + W * (Y + dy));
                    dot.OnInteract += ButtonPress(dot, dx + Width * dy);
                }

            var frameMeshName = string.Format("Frame{0}x{1}", Width, Height);
            prefab.Frame.sharedMesh = prefab.FrameMeshes.First(m => m.name == frameMeshName);
            var backMeshName = string.Format("Back{0}x{1}", Width, Height);
            prefab.Back.sharedMesh = prefab.BackMeshes.First(m => m.name == backMeshName);

            Module.StartCoroutine(Flicker(prefab.Text));
            prefab.Text.text = "ABCDEFG".Substring(Function, 1);
            Selection = -1;

#if UNITY_EDITOR
            for (int i = 0; i < Width * Height; i++)
                if (States[i] >= 0)
                    _buttons[i].GetComponent<MeshRenderer>().material.color = Color.blue;
#endif
        }

        private IEnumerator Flicker(TextMesh text)
        {
            while (true)
            {
                text.color = UnityEngine.Random.ColorHSV(0f, 0f, 0f, 0f, 0.6f, 1f, 1f, 1f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(.05f, .2f));
            }
        }

        private KMSelectable.OnInteractHandler ButtonPress(KMSelectable button, int btnIx)
        {
            return delegate
            {
                button.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);

                SetState(States[btnIx]);
                foreach (var btn in _buttons)
                    btn.GetComponent<MeshRenderer>().material.color = new Color32(0x88, 0x88, 0x88, 0xff);
                _buttons[btnIx].GetComponent<MeshRenderer>().material.color = Color.red;
#if UNITY_EDITOR
                for (int i = 0; i < Width * Height; i++)
                    if (States[i] >= 0)
                        _buttons[i].GetComponent<MeshRenderer>().material.color = btnIx == i ? Color.magenta : Color.blue;
#endif

                Selection = btnIx;
                return false;
            };
        }

        public override string ToString() { return string.Format("{0}×{1} coordinate grid displaying {2}", Width, Height, "ABCDEFG".Substring(Function, 1)); }
        public override object Flavor { get { return string.Format("CoordinateGrid:{0}:{1}", Width, Height); } }
        public override string DescribeSolutionState(int state) { return string.Format("select ({1},{0}) in the {2}×{3} coordinate grid", Height - 1 - Array.IndexOf(States, state) / Width, Array.IndexOf(States, state) % Width, Width, Height); }
        public override string DescribeWhatUserDid() { return string.Format("you selected a coordinate in the {0}×{1} coordinate grid", Width, Height); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have selected ({1},{0}) in the {2}×{3} coordinate grid (instead of {4})", Height - 1 - Array.IndexOf(States, desiredState) / Width, Array.IndexOf(States, desiredState) % Width, Width, Height, Selection == -1 ? "nothing" : string.Format("({1},{0})", Height - 1 - Selection / Width, Selection % Width)); }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, string.Format(@"^\s*{0}[x×]{1}\s+(?:coord(?:inate)?\s*-?\s*)?grid\s+(\d+)\s*$", Width, Height), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int val;
            if (m.Success && int.TryParse(m.Groups[1].Value, out val) && val > 0 && val <= Width * Height)
                return TwitchPress(val - 1).GetEnumerator();
            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            if (State == desiredState)
                return Enumerable.Empty<object>();

            return TwitchPress(Array.IndexOf(States, desiredState));
        }

        private IEnumerable<object> TwitchPress(int move)
        {
            _buttons[move].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}