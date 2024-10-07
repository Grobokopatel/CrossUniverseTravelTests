using System.Collections.Generic;
using System;
using System.Linq;

namespace CrossUniverseTravelTests
{
    // Класс для хранения координат звёзд
    public struct Point
    {
        public double X;
        public double Y;
        public double Z;

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static double GetDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }

        public static Point operator *(double d, Point p)
        {
            return new Point(d * p.X, d * p.Y, d * p.Z);
        }
    }

    public class Program
    {
        public static void Main()
        {
            // Я иногда звёзды называю точками, это одно и то же.
            // Расстояние и hops это тоже одно и то же.
            // TODO Наверное надо завести словарик, чтобы все пользовались одинаковыми терминами.

            // Это мне для отладки, у меня консоль в Райдере иногда не работает
            Console.WriteLine("Here");
            // Количество звёзд
            const int pointsCount = 1000;
            // Максимальное расстояние на которое может прыгнуть корабль
            const double r = 0.15;
            // У меня все звёзды располагаются в кубе со стороной fieldSideLength и у всех звёзд неотрицательные координаты.
            const double fieldSideLength = 1;
            // Длина стороны ячейки сетки. Минимальное, при котором всё будет правильно работать - r. Максимальное - fieldSideLength.
            const double gridCellLength = r;
            // Количество итераций
            const int testsCount = 10;

            TestThatGetHopsCountIsCorrect(pointsCount, r, gridCellLength, fieldSideLength, testsCount);

            // WriteHopsOrdered(pointsCount, r, gridCellLength, fieldSideLength);
        }

        /// <summary>
        /// Метод для отладки, чтобы сгенерировать случайные точки, посчитать расстояние и вывести в отсортированном
        /// по расстоянию порядке
        /// </summary>
        public static void WriteHopsOrdered(int pointsCount, double r, double gridCellLength, double fieldSideLength)
        {
            var hops = GetHopsCount(GetRandomPoints(pointsCount, fieldSideLength), r, fieldSideLength, gridCellLength);
            var ordered = hops.Select((d, i) => (d, i))
                .OrderBy(di => di.d)
                .ToArray();

            for (int i = 0; i < hops.Length; i++)
            {
                Console.WriteLine($"Звезда {ordered[i].i}. Расстояние: {ordered[i].d}");
            }
        }

        private static void TestThatGetHopsCountIsCorrect(int pointsCount, double r,
            double gridCellLength, double fieldSideLength, int testsCount)
        {
            var getHopsCountIsCorrect = true;
            for (var i = 0; i < testsCount; ++i)
            {
                // У меня все звёзды пронумерованы от 0 до pointsCount - 1
                var points = GetRandomPoints(pointsCount, fieldSideLength);
                // Наш способ подсчёта расстояния
                var hops = GetHopsCount(points, r, fieldSideLength, gridCellLength);
                // Подсчёт расстояния в тупую (точно работает правильно)
                var hopsDumb = GetHopsCountDumb(points, r);

                for (int j = 0; j < points.Length; j++)
                {
                    if (hops[j] != hopsDumb[j])
                    {
                        Console.WriteLine(
                            $"Неправильно расстояние для {i} итерации {j} звезды. Расстояние: {hops[j]}. Ожидаемое: {hopsDumb[j]}");
                        getHopsCountIsCorrect = false;
                    }
                }
            }

            if (getHopsCountIsCorrect)
            {
                Console.WriteLine($"Все {testsCount} итераций вернули одинаковые результаты");
            }
        }

        public static Point[] GetRandomPoints(int pointCount, double fieldSideLength)
        {
            var points = new Point[pointCount];

            var rng = new Random();
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = fieldSideLength * new Point(rng.NextDouble(), rng.NextDouble(), rng.NextDouble());
            }

            return points;
        }

        /// <summary>
        /// Тот способ подсчёта расстояния, который мы используем в коллабе
        /// </summary>
        public static int[] GetHopsCount(Point[] points, double r, double fieldSideLength, double gridCellLength)
        {
            // [x,y,z]-тый элемент grid содержит все индексы звёзд, принадлежащие к этой ячейке
            var grid = GetGrid(points, fieldSideLength, gridCellLength);
            // i-тый элемент adjacentPoints содержит все инцидентные звёзды i-той звезды
            var adjacentPoints = GetAdjacentPoints(grid, points, r);
            var hops = Dfs(adjacentPoints);

            return hops;
        }

        /// <summary>
        /// Тупой способ подсчёта расстояние перебором
        /// </summary>
        public static int[] GetHopsCountDumb(Point[] points, double r)
        {
            return Dfs(GetAdjacentPointsDumb(points, r));
        }


        /// <summary>
        /// По массиву координат точек вернуть массив списков, где i-тый список - инциндентные точки i-той точки
        /// </summary>
        public static List<int>[] GetAdjacentPoints(List<int>[,,] grid, Point[] points, double r)
        {
            // Так как все звёзды располагаются в кубе, то сетка имеет размер n на n на n, где n = fieldSideLength / gridCellLength  
            var gridDiameter = grid.GetLength(0);
            var adjacentPoints = new List<int>[points.Length];

            for (var i = 0; i < gridDiameter; ++i)
            {
                for (var j = 0; j < gridDiameter; ++j)
                {
                    for (var k = 0; k < gridDiameter; ++k)
                    {
                        var currentCell = grid[i, j, k];
                        if (currentCell != null)
                        {
                            for (var xDelta = -1; xDelta <= 1; ++xDelta)
                            {
                                for (var yDelta = -1; yDelta <= 1; ++yDelta)
                                {
                                    for (var zDelta = -1; zDelta <= 1; ++zDelta)
                                    {
                                        if (i + xDelta >= 0 && i + xDelta < gridDiameter
                                                            && j + yDelta >= 0 && j + yDelta < gridDiameter
                                                            && k + zDelta >= 0 && k + zDelta < gridDiameter)
                                        {
                                            var incidentCell = grid[i + xDelta, j + yDelta, k + zDelta];

                                            if (incidentCell != null)
                                            {
                                                foreach (var i1 in currentCell)
                                                {
                                                    var p1 = points[i1];
                                                    foreach (var i2 in incidentCell)
                                                    {
                                                        var p2 = points[i2];
                                                        if (Point.GetDistance(p1, p2) <= r)
                                                        {
                                                            adjacentPoints[i1] ??= new List<int>(4);
                                                            adjacentPoints[i1].Add(i2);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return adjacentPoints;
        }

        /// <summary>
        /// По массиву координат точек вернуть массив списков, где i-тый список - инциндентные точки i-той точки
        /// </summary>
        public static List<int>[] GetAdjacentPointsDumb(Point[] points, double r)
        {
            var adjacentPoints = new List<int>[points.Length];
            for (int i = 0; i < points.Length - 1; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    if (Point.GetDistance(points[i], points[j]) <= r)
                    {
                        // capacity = 4, чтобы слишком много памяти не занимать (не уверен, что это вообще нужно)
                        adjacentPoints[i] ??= new List<int>(4);
                        adjacentPoints[j] ??= new List<int>(4);

                        adjacentPoints[i].Add(j);
                        adjacentPoints[j].Add(i);
                    }
                }
            }

            return adjacentPoints;
        }


        /// <summary>
        /// Поиск в ширину, где на каждом шаге я прибавляю 1 к предыдущему посчитанному расстоянию
        /// </summary>
        public static int[] Dfs(List<int>[] adjacentPoints)
        {
            var distance = new int[adjacentPoints.Length];
            Array.Fill(distance, -1);
            distance[0] = 0;
            var visited = new bool[adjacentPoints.Length];
            var planned = new Queue<int>();

            planned.Enqueue(0);
            visited[0] = true;

            while (planned.Count != 0)
            {
                var current = planned.Dequeue();
                if (adjacentPoints[current] != null)
                {
                    foreach (var adjacent in adjacentPoints[current])
                    {
                        if (visited[adjacent] == false)
                        {
                            planned.Enqueue(adjacent);
                            visited[adjacent] = true;
                            distance[adjacent] = distance[current] + 1;
                        }
                    }
                }
            }

            return distance;
        }


        /// <summary>
        /// По массиву точек получить массив списков, где [x,y,z]-тый список это точки, принадлежащие к этой ячейке сетки
        /// </summary>
        public static List<int>[,,] GetGrid(Point[] points, double fieldSideLength, double gridCellLength)
        {
            // Так как все звёзды располагаются в кубе, то сетка имеет размер n на n на n, где n = fieldSideLength / gridCellLength  
            var gridSize = (int)Math.Ceiling(fieldSideLength / gridCellLength);
            // TODO поменять на  var grid = new List<int>[gridSize * 2, gridSize * 2, gridSize * 2];? а снизу прибавлять gridSize?
            // TODO Вроде не будет работать, если точки в случайных местах, а не привязаны к началу координат
            var grid = new List<int>[gridSize, gridSize, gridSize];

            for (int i = 0; i < points.Length; i++)
            {
                var currentPoint = points[i];

                int GetCellCoord(double coord)
                {
                    // Использовал Truncate вместо Round, потому что с Round пришлось бы делать дополнительные проверки, чтобы
                    // не вылезти за границы массива. По сути, без разницы, что использовать, надо только, чтобы все
                    // ячейки сетки имели размер >= r
                    return (int)Math.Truncate(coord);
                }

                var gridCellX = GetCellCoord(currentPoint.X / gridCellLength);
                var gridCellY = GetCellCoord(currentPoint.Y / gridCellLength);
                var gridCellZ = GetCellCoord(currentPoint.Z / gridCellLength);

                ref var gridSlot = ref grid[gridCellX, gridCellY, gridCellZ];
                gridSlot ??= new List<int>(4);

                gridSlot.Add(i);
            }

            return grid;
        }
    }
}