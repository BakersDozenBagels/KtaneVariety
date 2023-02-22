using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Variety
{
    // Dice:
    //
    // n is 24. Rotate the die such that you can see the face labeled as the value modulo 6, and the face with the pth smallest label is facing the status light, where p is the value divided by 6 (rounded down).
    public class Die : Item
    {
        private DiePrefab _prefab;
        private int _topLeftCell;
        private int _top, _turn;
        private Quaternion _trueRot = Quaternion.identity;

        public Die(VarietyModule module, int tlc) : base(module, CellRect(tlc, 2, 2))
        {
            _topLeftCell = tlc;
            SetState(-1, automatic: true);
        }

        private static readonly int[][] _turns = new int[][]
        {
                new int[] { 1, 2, 4, 3 },
                new int[] { 0, 3, 5, 2 },
                new int[] { 0, 1, 5, 4 },
                new int[] { 0, 4, 5, 1 },
                new int[] { 5, 3, 0, 2 },
                new int[] { 1, 3, 4, 2 },
        };

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.DieTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(_topLeftCell, 2), .015f, GetYOfCellRect(_topLeftCell, 2));
            _prefab.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            _prefab.transform.localRotation = Quaternion.identity;

            Color c1 = Color.HSVToRGB(Random.value, Random.Range(0.1f, 0.2f), Random.Range(0.8f, 0.9f));
            Color c2 = Color.HSVToRGB(Random.value, Random.Range(0.5f, 0.6f), Random.Range(0.1f, 0.2f));
            var rends = _prefab.Model.GetComponentsInChildren<MeshRenderer>();
            foreach(var rend in rends)
            {
                rend.materials[0].color = c1;
                if(rend.materials.Length >= 2)
                    rend.materials[1].color = c2;
            }

            Quaternion[] rots = new Quaternion[]
            {
                Quaternion.identity, // 1 0
                Quaternion.AngleAxis(90f, new Vector3(1f, 0f, 0f)), // 0 4
                Quaternion.AngleAxis(-90f, new Vector3(1f, 0f, 0f)), // 5 1
                Quaternion.AngleAxis(90f, new Vector3(0f, 0f, 1f)), // 3 0
                Quaternion.AngleAxis(-90f, new Vector3(0f, 0f, 1f)), // 2 0
                Quaternion.AngleAxis(180f, new Vector3(1f, 0f, 0f)) // 4 5
            };
            int r = Random.Range(0, 6);
            _turn = Random.Range(0, 4);
            _turn = 1;

            Quaternion rot2 = Quaternion.AngleAxis(_turn * 90f, new Vector3(0f, 1f, 0f)); // 1 = cw

            _prefab.Model.transform.localRotation = _trueRot = rot2 * rots[r];
            _top = new int[] { 1, 0, 5, 3, 2, 4 }[r];
            SetState(DigitsToState(_top, _turns[_top][_turn]), automatic: true);

            for(var i = 0; i < 4; i++)
            {
                _prefab.Selectables[i].OnInteract = ArrowPressed(i);
                yield return new ItemSelectable(_prefab.Selectables[i], Cells[0] + (i % 2) + W * (i / 2));
            }
        }

        private int DigitsToState(int up, int sl)
        {
            switch(up + "" + sl)
            {
                case "01":
                    return 0;
                case "02":
                    return 6;
                case "03":
                    return 12;
                case "04":
                    return 18;
                case "10":
                    return 1;
                case "12":
                    return 7;
                case "13":
                    return 13;
                case "15":
                    return 19;
                case "20":
                    return 2;
                case "21":
                    return 8;
                case "24":
                    return 14;
                case "25":
                    return 20;
                case "30":
                    return 3;
                case "31":
                    return 9;
                case "34":
                    return 15;
                case "35":
                    return 21;
                case "40":
                    return 4;
                case "42":
                    return 10;
                case "43":
                    return 16;
                case "45":
                    return 22;
                case "51":
                    return 5;
                case "52":
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
                switch(arrowIx)
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
                        int tp = _top;
                        _top = _turns[_top][_turn];
                        _turn = Array.IndexOf(_turns[_top], 5 - tp);
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

        private IEnumerator Animate(Transform tr, Quaternion end)
        {
            float t = Time.time;
            Quaternion start = tr.localRotation;
            while(Time.time - t < 0.25f)
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
                return "Die";
            }
        }

        public override string ToString()
        {
            return "Die";
        }

        public override string TwitchHelpMessage
        {
            get
            {
                return "!{0} die 1234 [press the rotation buttons; buttons are numbered in reading order]";
            }
        }

        public override string DescribeSolutionState(int state)
        {
            int top = state % 6;
            int sl = new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != top && i != 5 - top).ToArray()[state / 6];
            return string.Format("rotate the die so you can see the {0} side and the {1} side is facing the status light", top, sl);
        }

        public override string DescribeWhatUserDid()
        {
            return "you rotated the die";
        }

        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            int top = desiredState % 6;
            int sl = new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != top && i != 5 - top).ToArray()[desiredState / 6];
            return string.Format("you should have rotated the die so you can see the {0} side and the {1} side is facing the status light (you can see the {2} side and the {3} side is facing the status light)", top, sl, _top, _turns[_top][_turn]);
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {

            var m = Regex.Match(command, @"^\s*die\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if(m.Success && m.Groups[1].Value.All(ch => ch >= '1' && ch <= '4'))
            {
                foreach(var sel in m.Groups[1].Value.Select(c => _prefab.Selectables[c - '1']))
                {
                    sel.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield break;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            if(State == desiredState)
                return Enumerable.Empty<object>();

            var visited = new Dictionary<int, int>();
            var dirs = new Dictionary<int, int>();
            var q = new Queue<int>();
            q.Enqueue(State);

            while(q.Count > 0)
            {
                var item = q.Dequeue();
                var adjs = new List<int>();

                int stop = item % 6;
                int sturn = Array.IndexOf(_turns[stop], new int[] { 0, 1, 2, 3, 4, 5 }.Where(i => i != stop && i != 5 - stop).ToArray()[item / 6]);
                int top, turn;


                top = _turns[stop][(sturn + 3) % 4];
                turn = Array.IndexOf(_turns[top], _turns[stop][sturn]);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][(sturn + 2) % 4];
                turn = Array.IndexOf(_turns[top], stop);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][sturn];
                turn = Array.IndexOf(_turns[top], 5 - stop);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                top = _turns[stop][(sturn + 1) % 4];
                turn = Array.IndexOf(_turns[top], _turns[stop][sturn]);
                adjs.Add(DigitsToState(top, _turns[top][turn]));

                for(int i = 0; i < adjs.Count; i++)
                {
                    int adj = adjs[i];
                    int j = i;
                    if(adj != State && !visited.ContainsKey(adj))
                    {
                        visited[adj] = item;
                        dirs[adj] = j;
                        if(adj == desiredState)
                            goto done;
                        q.Enqueue(adj);
                    }
                }
            }
        done:
            var moves = new List<int>();
            var curPos = desiredState;
            var iter = 0;
            while(curPos != State)
            {
                iter++;
                if(iter > 100)
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
            for(int i = 0; i < moves.Count; i++)
            {
                _prefab.Selectables[moves[i]].OnInteract();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}