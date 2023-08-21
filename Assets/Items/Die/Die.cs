using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Variety
{
    public class Die : Item
    {
        private DiePrefab _prefab;
        private int _topLeftCell;
        private int _top, _turn;
        private Quaternion _trueRot = Quaternion.identity;
        private static readonly Vector3 SLPosition = new Vector3(0.075167f, 0f, 0.076057f);
        private bool _flavor;

        public Die(VarietyModule module, int tlc, bool flavor) : base(module, CellRect(tlc, 2, 2))
        {
            _topLeftCell = tlc;
            SetState(-1, automatic: true);
            _flavor = flavor;
        }

        private static readonly int[][] _turns = new int[][]
        {
                new int[] { 2, 4, 5, 3 },
                new int[] { 5, 4, 2, 3 },
                new int[] { 1, 4, 0, 3 },
                new int[] { 5, 1, 2, 0 },
                new int[] { 5, 0, 2, 1 },
                new int[] { 0, 4, 1, 3 },
        };

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.DieTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(_topLeftCell, 2), .015f, GetYOfCellRect(_topLeftCell, 2));
            _prefab.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            _prefab.transform.localRotation = Quaternion.FromToRotation(new Vector3(1f, 0f, 1f), SLPosition - new Vector3(_prefab.transform.localPosition.x, 0f, _prefab.transform.localPosition.z));

            Color c1 = Color.HSVToRGB(Random.value, Random.Range(0.1f, 0.2f), Random.Range(0.8f, 0.9f));
            Color c2 = Color.HSVToRGB(Random.value, Random.Range(0.5f, 0.6f), Random.Range(0.1f, 0.2f));
            var rends = _prefab.Model.GetComponentsInChildren<MeshRenderer>();
            if (!_flavor)
            {
                Color tmp = c2;
                c2 = c1;
                c1 = tmp;
            }

            foreach (var rend in rends)
            {
                rend.materials[0].color = c1;
                if (rend.materials.Length >= 2)
                    rend.materials[1].color = c2;
            }

            Quaternion[] rots = new Quaternion[]
            {
                Quaternion.identity, // 1 5
                Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f)), // 5 0
                Quaternion.AngleAxis(-90f, new Vector3(1f, 0f, 0f)), // 2 1
                Quaternion.AngleAxis(90f, new Vector3(0f, 0f, 1f)), // 4 5
                Quaternion.AngleAxis(-90f, new Vector3(0f, 0f, 1f)), // 3 5
                Quaternion.AngleAxis(180f, new Vector3(1f, 0f, 0f)) // 0 2
            };
            int r = Random.Range(0, 6);
            _turn = Random.Range(0, 4);

            Quaternion rot2 = Quaternion.AngleAxis(_turn * 90f, new Vector3(0f, 1f, 0f)); // 1 = cw

            _prefab.Model.transform.localRotation = _trueRot = rot2 * rots[r];
            _top = new int[] { 1, 5, 2, 4, 3, 0 }[r];
            SetState(DigitsToState(_top, _turns[_top][_turn]), automatic: true);

            for (var i = 0; i < 4; i++)
            {
                _prefab.Selectables[i].OnInteract = ArrowPressed(i);
                yield return new ItemSelectable(_prefab.Selectables[i], Cells[0] + (i % 2) + W * (i / 2));
            }
        }

        private int DigitsToState(int up, int sl)
        {
            switch (up + "" + sl)
            {
                case "02":
                    return 0;
                case "03":
                    return 6;
                case "04":
                    return 12;
                case "05":
                    return 18;
                case "12":
                    return 1;
                case "13":
                    return 7;
                case "14":
                    return 13;
                case "15":
                    return 19;
                case "20":
                    return 2;
                case "21":
                    return 8;
                case "23":
                    return 14;
                case "24":
                    return 20;
                case "30":
                    return 3;
                case "31":
                    return 9;
                case "32":
                    return 15;
                case "35":
                    return 21;
                case "40":
                    return 4;
                case "41":
                    return 10;
                case "42":
                    return 16;
                case "45":
                    return 22;
                case "50":
                    return 5;
                case "51":
                    return 11;
                case "53":
                    return 17;
                case "54":
                    return 23;
            }

            Debug.LogError("<Variety #" + Module.ModuleID + "> Bad dice state " + up + "" + sl);

            return -1;
        }

        private KMSelectable.OnInteractHandler ArrowPressed(int arrowIx)
        {
            return delegate
            {
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _prefab.Selectables[arrowIx].transform);
                switch (arrowIx)
                {
                    case 0:
                        _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(0f, 0f, -90f) * _trueRot));
                        int p = _top;
                        _top = _turns[_top][(_turn + 3) % 4];
                        _turn = Array.IndexOf(_turns[_top], _turns[p][_turn]);
                        break;
                    case 1:
                        _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(-90f, 0f, 0f) * _trueRot));
                        int t = _top;
                        _top = _turns[_top][(_turn + 2) % 4];
                        _turn = Array.IndexOf(_turns[_top], t);
                        break;
                    case 2:
                        _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(90f, 0f, 0f) * _trueRot));
                        int q = _top;
                        _top = _turns[_top][_turn];
                        _turn = Array.IndexOf(_turns[_top], Flip(q));
                        break;
                    case 3:
                        _prefab.StartCoroutine(Animate(_prefab.Model, _trueRot = Quaternion.Euler(0f, 0f, 90f) * _trueRot));
                        int top = _top;
                        _top = _turns[_top][(_turn + 1) % 4];
                        _turn = Array.IndexOf(_turns[_top], _turns[top][_turn]);
                        break;
                }

                SetState(DigitsToState(_top, _turns[_top][_turn]));

                return false;
            };
        }

        private int Flip(int num)
        {
            num = 7 - num;
            if (num == 6)
                num = 0;
            if (num == 7)
                num = 1;
            return num;
        }

        private IEnumerator Animate(Transform tr, Quaternion end)
        {
            float t = Time.time;
            Quaternion start = tr.localRotation;
            while (Time.time - t < 0.25f)
            {
                tr.localRotation = Quaternion.Slerp(start, end, (Time.time - t) * 4f);
                tr.localPosition = new Vector3(0f, (0.125f - Mathf.Abs(Time.time - t - 0.125f)) * 0.1f + 0.009f);
                yield return null;
            }
            tr.localRotation = end;
            tr.localPosition = new Vector3(0f, 0.009f);
        }

        public override int NumStates
        {
            get
            {
                return 24;
            }
        }

        public override object Flavor
        {
            get
            {
                return _flavor ? "DieDOL" : "DieLOD";
            }
        }

        public override string ToString()
        {
            return string.Format("{0} die", _flavor ? "dark-on-light" : "light-on-dark");
        }

        public override string TwitchHelpMessage
        {
            get
            {
                return "!{0} light-on-dark die 1234 [press the rotation buttons; buttons are numbered from the one pointing towards the status light going clockwise]";
            }
        }

        public override string DescribeSolutionState(int state)
        {
            int top = state % 6;
            int sl = new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != top && i != Flip(top)).ToArray()[state / 6];
            return string.Format("rotate the {2} die so you can see the {0} side and the {1} side is facing the status light", top, sl, _flavor ? "dark-on-light" : "light-on-dark");
        }

        public override string DescribeWhatUserDid()
        {
            return string.Format("you rotated the {0} die", _flavor ? "dark-on-light" : "light-on-dark");
        }

        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            int top = desiredState % 6;
            int sl = new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != top && i != Flip(top)).ToArray()[desiredState / 6];
            return string.Format("you should have rotated the {4} die so you can see the {0} side and the {1} side is facing the status light (you can see the {2} side and the {3} side is facing the status light)", top, sl, _top, _turns[_top][_turn], _flavor ? "dark-on-light" : "light-on-dark");
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, (_flavor ? @"^\s*(?:dark-?on-?light|do?l)" : @"^\s*(?:light-?on-?dark|lo?d)") + @"\s+die\s+([1-4]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.ECMAScript);
            if (m.Success)
            {
                return TwitchPress(m.Groups[1].Value);
            }

            return null;
        }

        private IEnumerator TwitchPress(string b)
        {
            int[] shuffle = new int[] { 1, 3, 2, 0 };
            foreach (var sel in b.Select(c => _prefab.Selectables[shuffle[c - '1']]))
            {
                sel.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            if (State == desiredState)
                return Enumerable.Empty<object>();

            var visited = new Dictionary<int, int>();
            var dirs = new Dictionary<int, int>();
            var q = new Queue<int>();
            q.Enqueue(State);

            while (q.Count > 0)
            {
                var item = q.Dequeue();
                var adjs = new List<int>();

                int stop = item % 6;
                int sturn = Array.IndexOf(_turns[stop], new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != stop && i != Flip(stop)).ToArray()[item / 6]);
                int top, turn;

                top = _turns[stop][(sturn + 3) % 4];
                turn = Array.IndexOf(_turns[top], _turns[stop][sturn]);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][(sturn + 2) % 4];
                turn = Array.IndexOf(_turns[top], stop);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][sturn];
                turn = Array.IndexOf(_turns[top], Flip(stop));
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][(sturn + 1) % 4];
                turn = Array.IndexOf(_turns[top], _turns[stop][sturn]);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                for (int i = 0; i < adjs.Count; i++)
                {
                    int adj = adjs[i];
                    int j = i;
                    if (adj != State && !visited.ContainsKey(adj))
                    {
                        visited[adj] = item;
                        dirs[adj] = j;
                        if (adj == desiredState)
                            goto done;
                        q.Enqueue(adj);
                    }
                }
            }
            done:
            var moves = new List<int>();
            var curPos = desiredState;
            var iter = 0;
            while (curPos != State)
            {
                iter++;
                if (iter > 100)
                {
                    Debug.LogFormat("<> State = {0}", State);
                    Debug.LogFormat("<> desiredState = {0}", desiredState);
                    Debug.LogFormat("<> moves = {0}", moves.Join(","));
                    Debug.LogFormat("<> visited:\n{0}", visited.Select(kvp => string.Format("{0} <= {1}", kvp.Key, kvp.Value)).Join("\n"));
                    throw new InvalidOperationException();
                }

                moves.Add(dirs[curPos]);
                curPos = visited[curPos];
            }

            moves.Reverse();
            return TwitchMove(moves);
        }

        private IEnumerable<object> TwitchMove(List<int> moves)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                _prefab.Selectables[moves[i]].OnInteract();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}