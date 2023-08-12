namespace Sudoku.Data;

public static class ListExtensions {
    private static Random rng = new Random();  

    public static void Shuffle<T>(this IList<T> list)   {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }

    public static T Random<T>(this IList<T> list) {
        if (list.Count == 0)
            throw new IndexOutOfRangeException("Random selection requires at least one element");
        if (list.Count == 1)
            return list[0];
        return list[rng.Next(list.Count)];
    }

    public static T Random<T>(this ISet<T> set) {
        if (set.Count == 0)
            throw new IndexOutOfRangeException("Random selection requires at least one element");
        if (set.Count == 1)
            return set.First();
        return set.ElementAt(rng.Next(set.Count));
    }
}