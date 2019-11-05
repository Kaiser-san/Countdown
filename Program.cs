using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Countdown
{
    internal static class Program
    {
        private static void Main()
        {
            #region Input

            Console.WriteLine("Array size: ");
            int arraySize = Convert.ToInt32(Console.ReadLine());
            var numbers = new int[arraySize];
            Console.WriteLine("Array (one number per line): ");
            for (int i = 0; i < arraySize; i++)
                numbers[i] = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Target: ");
            int result = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Debug Mode: ");
            int debugMode = Convert.ToInt32(Console.ReadLine());

            Array.Sort(numbers, (i1, i2) => i1.CompareTo(i2));

            #endregion

            var sw = new Stopwatch();
            sw.Start();
            var answer = Countdown(result, numbers, debugMode);
            sw.Stop();
            Console.WriteLine("{0} = {1} ({2}ms)", answer, answer.Value, sw.ElapsedMilliseconds);

            Thread.Sleep(5000000);
        }

        private static ExpressionBase Countdown(int n, IEnumerable<int> numbers, int debugMode)
        {
            var expressions = new Dictionary<CollectionOfValues, List<ExpressionBase>>();

            ExpressionBase result = null;
            var currentBestDiff = int.MaxValue;

            foreach (var subsequence in SubSequences(numbers.ToList()))
            {
                if (debugMode == 1)
                    Console.WriteLine(string.Join(", ", subsequence.Select(o => o.ToString()).ToArray()));

                foreach (var expression in MakeExpressions(subsequence, expressions))
                {
                    if (debugMode == 1)
                        Console.WriteLine(expression + " = " + expression.Value);

                    var thisDiff = Math.Abs(n - expression.Value);

                    if (thisDiff == 0)
                        return expression;

                    if (thisDiff < currentBestDiff)
                    {
                        currentBestDiff = thisDiff;
                        result = expression;
                    }
                }
                if(debugMode == 1)
                    Console.ReadLine();
            }

            return result;
        }

        private static IEnumerable<IEnumerable<int>> SubSequences(List<int> numbers)
        {
            var firstNum = numbers[0];
            var firstEnumerable = Enumerable.Repeat(firstNum, 1);

            if (numbers.Count == 1)
                return Enumerable.Repeat(firstEnumerable, 1);

            numbers.RemoveAt(0);
            var restSubSequences = SubSequences(numbers);

            return restSubSequences.Concat(Enumerable.Repeat(firstEnumerable, 1).Concat(restSubSequences.Select(subSeq => firstEnumerable.Concat(subSeq))));
        }

        private static List<ExpressionBase> MakeExpressions(IEnumerable<int> numbers, Dictionary<CollectionOfValues, List<ExpressionBase>> expressions)
        {
            List<ExpressionBase> result;
            List<int> numbersList = numbers.ToList();
            CollectionOfValues collection = new CollectionOfValues(numbersList);
            if (expressions.TryGetValue(collection, out result))
                return result;

            if (numbersList.Count == 1)
                result = new List<ExpressionBase> { new Number(numbersList[0]) };

            else
            {
                result = new List<ExpressionBase>();
                foreach (var pair in Unmerges(numbers))
                    foreach (var expression1 in MakeExpressions(pair.Item1, expressions))
                        foreach (var expression2 in MakeExpressions(pair.Item2, expressions))
                            result.AddRange(Combine(expression1, expression2));
            }

            expressions.Add(collection, result);
            return result;
        }

        private static IEnumerable<Tuple<IEnumerable<int>, IEnumerable<int>>> Unmerges(IEnumerable<int> values)
        {
            var firstVal = values.ElementAt(0);
            var firstEnumerable = Enumerable.Repeat(firstVal, 1);

            if (values.Take(3).Count() == 2)
            {
                var secondVal = values.ElementAt(1);
                var secondEnumerable = Enumerable.Repeat(secondVal, 1);
                return Enumerable.Repeat(Tuple.Create(firstEnumerable, secondEnumerable), 1);
            }

            var rest = values.Skip(1);
            var firstBit = Enumerable.Repeat(Tuple.Create(firstEnumerable, rest), 1);
            var secondBit = Unmerges(rest).SelectMany(t => new[] { Tuple.Create(firstEnumerable.Concat(t.Item1), t.Item2), Tuple.Create(t.Item1, firstEnumerable.Concat(t.Item2)) });

            return firstBit.Concat(secondBit);
        }

        private static IEnumerable<ExpressionBase> Combine(ExpressionBase expression1, ExpressionBase expression2)
        {
            int value1 = expression1.Value;
            int value2 = expression2.Value;

            if (value1 < value2) return Comb1(expression1, expression2);
            if (value1 == value2) return Comb2(expression1, expression2);
            return Comb1(expression2, expression1);
        }

        private static IEnumerable<ExpressionBase> Comb1(ExpressionBase expression1, ExpressionBase expression2)
        {
            int value1 = expression1.Value;
            int value2 = expression2.Value;
            OperatorType operator1 = expression1.Operator;
            OperatorType operator2 = expression2.Operator;
            if (operator1 != OperatorType.Sub && operator2 != OperatorType.Sub)
            {
                if (operator2 != OperatorType.Add)
                    yield return new Expression(OperatorType.Add, expression1, expression2, value1 + value2);

                yield return new Expression(OperatorType.Sub, expression2, expression1, value2 - value1);
            }

            if (1 < value1 && operator1 != OperatorType.Div && operator2 != OperatorType.Div)
            {
                if (operator2 != OperatorType.Mul)
                    yield return new Expression(OperatorType.Mul, expression1, expression2, value1 * value2);

                var q = value2 / value1;
                var r = value2 % value1;

                if (r == 0)
                    yield return new Expression(OperatorType.Div, expression2, expression1, q);
            }
        }

        private static IEnumerable<ExpressionBase> Comb2(ExpressionBase expression1, ExpressionBase expression2)
        {
            int value1 = expression1.Value;
            int value2 = expression2.Value;
            OperatorType operator1 = expression1.Operator;
            OperatorType operator2 = expression2.Operator;
            if (operator1 != OperatorType.Sub && operator2 != OperatorType.Add && operator2 != OperatorType.Sub)
                yield return new Expression(OperatorType.Add, expression1, expression2, value1 + value2);

            if (1 < value1 && operator1 != OperatorType.Div && operator2 != OperatorType.Div)
            {
                if (operator2 != OperatorType.Mul)
                    yield return new Expression(OperatorType.Mul, expression1, expression2, value1 * value2);

                yield return new Expression(OperatorType.Div, expression1, expression2, 1);
            }
        }

        #region Nested type: CollectionOfValues

        private class CollectionOfValues
        {
            private readonly List<int> array;

            public CollectionOfValues(List<int> array)
            {
                this.array = array;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                    return true;

                var other = obj as CollectionOfValues;
                if (other == null)
                    return false;

                if (other.array.Count != this.array.Count)
                    return false;

                for (int i = 0; i < this.array.Count; i++)
                    if (this.array[i] != other.array[i])
                        return false;
                return true;
            }

            public override int GetHashCode()
            {
                int hc = this.array.Count;
                for (int i = 0; i < this.array.Count; i++)
                    hc = unchecked(hc * 397 + this.array[i]);
                return hc;
            }
        }

        #endregion

        #region Nested type: Expression

        private class Expression : ExpressionBase
        {
            private readonly OperatorType operatorType;
            private readonly ExpressionBase subExpression1;
            private readonly ExpressionBase subExpression2;
            private readonly int value;

            public Expression(OperatorType operatorType, ExpressionBase subExpression1, ExpressionBase subExpression2, int value)
            {
                this.operatorType = operatorType;
                this.subExpression1 = subExpression1;
                this.subExpression2 = subExpression2;
                this.value = value;
            }

            #region Properties

            internal override int Value
            {
                get { return this.value; }
            }

            internal override OperatorType Operator
            {
                get { return this.operatorType; }
            }

            #endregion

            public override string ToString()
            {
                return string.Format("({0}{1}{2})", this.subExpression1, this.operatorType.GetName(), this.subExpression2);
            }
        }

        #endregion

        #region Nested type: ExpressionBase

        private abstract class ExpressionBase
        {
            #region Properties

            internal abstract int Value { get; }

            internal abstract OperatorType Operator { get; }

            #endregion
        }

        #endregion

        #region Nested type: Number

        private class Number : ExpressionBase
        {
            private readonly int value;

            public Number(int value)
            {
                this.value = value;
            }

            #region Properties

            internal override int Value
            {
                get { return this.value; }
            }

            internal override OperatorType Operator
            {
                get { return OperatorType.None; }
            }

            #endregion

            public override string ToString()
            {
                return this.value.ToString();
            }
        }

        #endregion
    }

    internal enum OperatorType
    {
        None,
        Add,
        Sub,
        Mul,
        Div
    }

    internal static class OperatorTypeExtender
    {
        internal static string GetName(this OperatorType type)
        {
            switch (type)
            {
                case OperatorType.Add:
                    return "+";
                case OperatorType.Sub:
                    return "-";
                case OperatorType.Mul:
                    return "*";
                case OperatorType.Div:
                    return "/";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}