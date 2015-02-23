using System;
using System.Data;

namespace HE.Gui
{
    public class Ref<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        public Ref(Func<T> getter, Action<T> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public override string ToString()
        {
            return _getter().ToString();
        }

        public T Value { get { return _getter(); } set { _setter(value); } }
    } 

    public class BindingHelper
    {
        public static DataView GetBindable2DArray<T>(T[,] array)
        {
            var dataTable = new DataTable();
            for (int i = 0; i < array.GetLength(1); i++)
            {
                dataTable.Columns.Add(i.ToString(), typeof(Ref<T>));
            }
            for (int i = 0; i < array.GetLength(0); i++)
            {
                var dataRow = dataTable.NewRow();
                dataTable.Rows.Add(dataRow);
            }
            var dataView = new DataView(dataTable);
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    int a = i;
                    int b = j;
                    var refT = new Ref<T>(() => array[a, b], z => { array[a, b] = z; });
                    dataView[i][j] = refT;
                }
            }
            return dataView;
        }
    }
}