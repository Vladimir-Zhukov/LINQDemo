using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LINQDemo
{
    partial class ProjectDataModel
    {
        public Dictionary<string, bool?> ModifiedProperties =
                 new Dictionary<string, bool?>();

        /// <summary>
        /// Перехват события создание объекта
        /// </summary>
        partial void OnCreated()
        {
            PropertyChanged += HandlePropertyChangedEvent;

            //значение по умолчанию
            if (CreationDate == DateTime.MinValue)
                CreationDate = DateTime.Now;
        }

        /// <summary>
        /// Регистрация изменений 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandlePropertyChangedEvent(object sender, PropertyChangedEventArgs e)
        {
            //Console.WriteLine("Property is changed: {0}", e.PropertyName);
            ModifiedProperties[e.PropertyName] = true;
        }
        

        /// <summary>
        /// Валидация перед сохранением
        /// </summary>
        /// <param name="action"></param>
        partial void OnValidate(System.Data.Linq.ChangeAction action)
        {
            if(string.IsNullOrEmpty(Name))
                throw new Exception("Название проекта не может быть пустым!");
        }
    }
}
