﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace TacticalAgro {
    public class TacticalMap : INotifyPropertyChanged {
        private Obstacle[] obstacles;
        private Base[] bases;
        private Size borders;
        private string path;

        public event PropertyChangedEventHandler? PropertyChanged;

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlIgnore]
        public string Path { get; set; }
        [XmlArray("Obstacles")]
        [XmlArrayItem("Obstacle")]
        public Obstacle[] Obstacles { get => obstacles;
            set {
                obstacles = value;
                PropertyChanged?.Invoke(Obstacles, new PropertyChangedEventArgs(nameof(Obstacles)));
            }
        }
        [XmlArray("Bases")]
        [XmlArrayItem("Base")]
        public Base[] Bases { 
            get => bases;
            set { 
                bases = value;
                PropertyChanged?.Invoke(Bases, new PropertyChangedEventArgs(nameof(Bases)));
            } 
        }
        public System.Windows.Size Borders { 
            get => borders;
            set {
                borders = value;
                PropertyChanged?.Invoke(Borders, new PropertyChangedEventArgs(nameof(Borders)));
            }
        }

        public void Save() {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(TacticalMap));
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                xmlSerializer.Serialize(fs, this);
                fs.Close();
            }
        }
        
        public TacticalMap() {
            Obstacles = new Obstacle[0];
            Bases = new Base[0];
            Borders = new Size(0, 0);
        }
        public TacticalMap(Obstacle[] _obstacles, Base[] _bases, Size _borders) {
            Obstacles = _obstacles;
            Bases = _bases;
            Borders = _borders;
        }

        public TacticalMap(string path) {
            if (File.Exists(path)) {
                TacticalMap newMap;
                using (FileStream fs = new FileStream(path, FileMode.Open)) {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(TacticalMap));
                    newMap = (TacticalMap)xmlSerializer.Deserialize(fs);
                    fs.Close();
                }
                this.Obstacles = newMap.Obstacles;
                this.Bases = newMap.Bases;
                this.Borders = newMap.Borders;
                this.Name = newMap.Name;
            } else 
                throw new FileNotFoundException();
            Path = path;
        }
    }
}
