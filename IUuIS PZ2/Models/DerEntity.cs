using IUuIS_PZ2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IUuIS_PZ2.Models
{
    public class DerEntity : ObservableObject
    {
        private int _id;
        private string _name = "";
        private DerType _type;
        private double _lastValue;
        private bool _isValid = true;

        public int Id { get => _id; set => Set(ref _id, value); }
        public string Name { get => _name; set => Set(ref _name, value); }
        public DerType Type { get => _type; set => Set(ref _type, value); }
        public double LastValue { get => _lastValue; set => Set(ref _lastValue, value); }
        public bool IsValid { get => _isValid; set => Set(ref _isValid, value); }
    }
}
