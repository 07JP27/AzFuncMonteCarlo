namespace AzFuncMonteCarlo
{
    public static class Utility
    {
        public static (double, double) GenerateRandomPoint()
        {
            var rand = new Random();
            double x = rand.NextDouble();
            double y = rand.NextDouble();
            return (x, y);
        }

        public static bool InCircle(double x, double y)
        {
            return (x * x + y * y <= 1);
        }

        public static double Median(this List<double> list)
        {
            var sortedList = list.OrderBy(x => x).ToList();
            int count = sortedList.Count;
            if (count % 2 == 0)
            {
                return (sortedList[count / 2 - 1] + sortedList[count / 2]) / 2;
            }
            else
            {
                return sortedList[count / 2];
            }
        }

         public static double Mode(this List<double> list)
         {
                var mode = list.GroupBy(x => x)
                    .OrderByDescending(x => x.Count())
                    .First()
                    .Key;
                return mode;
         }
    }
}