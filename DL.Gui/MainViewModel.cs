using System;
using System.Data;
using System.Windows.Input;
using ExpressionEvaluator;
using HE.Logic;
using Microsoft.TeamFoundation.MVVM;

namespace HE.Gui
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
            PopulateTestTaskCommand = new RelayCommand(PopulateTestTask);
            PopulateMainTaskCommand = new RelayCommand(PopulateMainTask);
            MajorBoundX = 1;
            IterationsMax = 1;
            IntervalsY = 100;
            IntervalsX = 10;
            MinorBoundY = 0.0;
            MajorBoundY = 0.0;
            Function = "0.0";
        }

        public string Function { get; set; }

        public ICommand CalculateCommand { get; set; }
        public ICommand PopulateMainTaskCommand { get; set; }
        public ICommand PopulateTestTaskCommand { get; set; }

        public double MinorBoundX { get; set; }
        public double MajorBoundX { get; set; }
        public int IterationsMax { get; set; }
        public int IntervalsX { get; set; }
        public int IntervalsY { get; set; }
        public double MinorBoundY { get; set; }
        public double MajorBoundY { get; set; }

        public DataView ResultGrid { get; set; }

        public double IterationMethodAccuracy { get; set; }
        public double SoultionAccuracy { get; set; }

        public int IterationsPassed { get; set; }

        public double EpsilonStopCondition { get; set; }

        public bool ShowResultGrid { get; set; }

        private void PopulateMainTask()
        {
            MinorBoundX = 1;
            MajorBoundX = 2;
            
            MinorBoundY = 2;
            MajorBoundY = 3;

            IntervalsX = 4;
            IntervalsY = 4;

            IterationsMax = 1;
            EpsilonStopCondition = 0.0001;

            Function = "m.Sin(m.PI*p.x*p.y)*m.PI*m.PI*(p.x*p.x + p.y*p.y)";
            Solution = "m.Sin(m.PI*p.x*p.y)";

            RaisePropertyChanged(null);
        }

        private void PopulateTestTask()
        {
            MinorBoundX = -1;
            MajorBoundX = 1;

            MinorBoundY = -1;
            MajorBoundY = 1;

            Function = "4.0";
            Solution = "1.0 - p.x * p.x - p.y * p.y";
            IterationsMax = 1;
            EpsilonStopCondition = 0.0001;

            IntervalsX = 4;
            IntervalsY = 4;

            RaisePropertyChanged(null);
        }

        private void Calculate()
        {
//            var solver = new HeatEquationSolver
//            {
//                LeftBoundary = MinorBoundX,
//                RightBoundary = MajorBoundX,
//                LeftBoundCondition = Parser.ParseTimeArgMethod(MinorBoundY),
//                RightBoundCondition = Parser.ParseTimeArgMethod(MajorBoundY),
//                StartCondition = Parser.ParsePositionArgMethod(InitialCondition),
//                Function = Parser.ParseTwoArgsMethod(Function)
//            };
//            var answer = solver.Solve(IterationsMax, IntervalsX, IntervalsY);
            double h = (MajorBoundX - MinorBoundX)/IntervalsX;
            double k = (MajorBoundY - MinorBoundY)/IntervalsY;
            int nodesX = IntervalsX + 1;
            int nodesY = IntervalsY + 1;
            var v = new double[nodesX, nodesY];
            Func<double, double> getX = i => MinorBoundX + i*h;
            Func<double, double> getY = i => MinorBoundY + i*k;
            
            Func<double, double, double> solution = Parser.ParseTwoArgsMethod(Solution);
            //Func<double, double, double> solution = (x, y) => Math.Sin(Math.PI*x*y);

            for (int i = 0; i < nodesX; i++)
            {
                v[i, 0] = solution(getX(i), getY(0));
                v[i, IntervalsY] = solution(getX(i), getY(IntervalsY));
            }
            for (int j = 0; j < nodesY; j++)
            {
                v[0, j] = solution(getX(0), getY(j));
                v[IntervalsX, j] = solution(getX(IntervalsX), getY(j));
            }
            double h2 = 1.0/(h*h);
            double k2 = 1.0/(k*k);
            double a = 2*(h2 + k2);

            Func<double, double, double> f = Parser.ParseTwoArgsMethod(Function);
            double iterationEpsilonMax = 0.0;

            int currentIteration = 0;

            while (true)
            {
                iterationEpsilonMax = 0.0;
                for (int j = 1; j < IntervalsY; j++)
                {
                    for (int i = 1; i < IntervalsX; i++)
                    {
                        double previousResult = v[i, j];
                        double fVal = f(getX(i), getY(j));
                        double currentResult = h2*(v[i - 1, j] + v[i + 1, j]) + k2*(v[i, j - 1] + v[i, j + 1]) + fVal;
                        currentResult = currentResult/a;
                        v[i, j] = currentResult;
                        double iterationEpsilon = Math.Abs(previousResult - currentResult);
                        if (iterationEpsilon > iterationEpsilonMax)
                        {
                            iterationEpsilonMax = iterationEpsilon;
                        }
                    }
                }
                currentIteration++;
                if (EpsilonStopCondition > iterationEpsilonMax)
                {
                    break;
                }
                if (currentIteration >= IterationsMax)
                {
                    break;
                }
            }

            IterationsPassed = currentIteration;
            IterationMethodAccuracy = iterationEpsilonMax;
            double maxSolutionDifference = 0;
            for (int j = 0; j < nodesY; j++)
            {
                for (int i = 0; i < nodesX; i++)
                {
                    double currentDifference = Math.Abs(v[i, j] - solution(getX(i), getY(j)));
                    maxSolutionDifference = Math.Max(maxSolutionDifference, currentDifference);
                }
            }
            SoultionAccuracy = maxSolutionDifference;
            if (ShowResultGrid)
            {
                ResultGrid = Populate(v);
            }
            else
            {
                ResultGrid = null;
            }
            
            RaisePropertyChanged(null);
        }

        public string Solution { get; set; }

        private DataView Populate(double[,] answer)
        {
            int lengthX = answer.GetLength(0);
            int lengthY = answer.GetLength(1);

            var reverted = new double[lengthY, lengthX];

            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    reverted[j, i] = answer[i, lengthY - 1 - j];
                }
            }
            return BindingHelper.GetBindableDoubleArray(reverted);
        }

        private DataView Populate(EquationSolveAnswer answer)
        {
            var result = new double[2, answer.Nodes.Length];
            for (int i = 0; i < answer.Nodes.Length; i++)
            {
                result[0, i] = answer.Nodes[i];
                result[1, i] = answer.LastLayer[i];
            }
            return BindingHelper.GetBindable2DArray(result);
        }
    }


    public class TwoArgs
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class TimeArg
    {
        public double t { get; set; }
    }

    public class PositionArg
    {
        public double x { get; set; }
    }

    public class Parser
    {
        public static Func<double, double> ParsePositionArgMethod(string textExpression)
        {
            var typeRegistry = new TypeRegistry();
            var param = new PositionArg();
            typeRegistry.RegisterType("m", typeof (Math));
            typeRegistry.RegisterSymbol("p", param);

            var expression = new CompiledExpression<double>(textExpression)
            {
                TypeRegistry = typeRegistry
            };

            Func<double, double> f = x =>
            {
                param.x = x;
                return expression.Eval();
            };
            return f;
        }

        public static Func<double, double> ParseTimeArgMethod(string textExpression)
        {
            var typeRegistry = new TypeRegistry();
            var param = new TimeArg();
            typeRegistry.RegisterType("m", typeof (Math));
            typeRegistry.RegisterSymbol("p", param);

            var expression = new CompiledExpression<double>(textExpression)
            {
                TypeRegistry = typeRegistry
            };

            Func<double, double> f = t =>
            {
                param.t = t;
                return expression.Eval();
            };
            return f;
        }

        public static Func<double, double, double> ParseTwoArgsMethod(string textExpression)
        {
            var typeRegistry = new TypeRegistry();
            var param = new TwoArgs();
            typeRegistry.RegisterType("m", typeof (Math));
            typeRegistry.RegisterSymbol("p", param);

            var expression = new CompiledExpression<double>(textExpression)
            {
                TypeRegistry = typeRegistry
            };

            Func<double, double, double> f = (x, y) =>
            {
                param.x = x;
                param.y = y;
                return expression.Eval();
            };
            return f;
        }
    }
}