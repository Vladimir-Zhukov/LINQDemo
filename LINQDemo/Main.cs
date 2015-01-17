using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LINQDemo
{
    class Main
    {
        internal void Execute(DemoCommands cmd)
        {
            //db.DeferredLoadingEnabled = false;
            //Класс DataLoadOptions обеспечивает два метода для достижения немедленной загрузки указанных связанных данных.
            //Метод LoadWith позволяет немедленно загрузить данные, которые относятся к основной цели.
            //Метод AssociateWith позволяет выполнять фильтрацию связанных объектов.
            switch (cmd)
            {
                case DemoCommands.Create:
                    TryCreate();
                    break;
                case DemoCommands.Update:
                    TryUpdate();
                    break;
                case DemoCommands.UpdateAlone:
                    TryUpdateAlone();
                    break;
                case DemoCommands.UpdateAll:
                    TryUpdateAll();
                    break;
                case DemoCommands.Delete:
                    TryDelete();
                    break;
                case DemoCommands.DeleteAlone:
                    TryDeleteAlone();
                    break;
                case DemoCommands.DeleteAll:
                    TryDeleteAll();
                    break;
                case DemoCommands.StoredProcedure:
                    TryCallSP();
                    break;
                case DemoCommands.Sql:
                    TryExecuteSql();
                    break;
                case DemoCommands.Log:
                    TryLog();
                    break;
                case DemoCommands.ChangeSet:
                    TryChangeSet();
                    break;
                case DemoCommands.GetCommand:
                    TryGetCommand();
                    break;
                case DemoCommands.Transaction:
                    TryTwoRecordsInsert();
                    break;
                case DemoCommands.Join:
                    TryJoin();
                    break;
                case DemoCommands.Paging:
                    TryPaging();
                    break;
                case DemoCommands.Aggregate:
                    TryCount();
                    break;
                case DemoCommands.DisableDeffered:
                    TryDisableDeffered();
                    break;
                case DemoCommands.EagerLoading:
                    TryEagerLoading();
                    TryEagerLoading2();
                    break;
            }
        }

        /// <summary>
        /// Демонстрация отключения отложенной загрузки
        /// </summary>
        private void TryDisableDeffered()
        {
            using (var db = new EbookDataContext())
            {
                db.DeferredLoadingEnabled = false;
                db.Log = new DebugTextWriter();
                foreach (var user in db.UserDataModels)
                {
                    //поле Name имеет свойство отложенной загрузки
                    Console.WriteLine("user: {0}", user.Name);
                }
            }
        }

        /// <summary>
        /// Жадная загрузка данных
        /// </summary>
        private void TryEagerLoading()
        {
            using (var db = new EbookDataContext())
            {
                var options = new DataLoadOptions();
                options.LoadWith<UserDataModel>(u => u.ProjectDataModels);
                db.LoadOptions = options;

                var users = db.UserDataModels;
                foreach (var user in users)
                {
                    Console.WriteLine("user: {0}", user.Name);
                    foreach (var pr in user.ProjectDataModels)
                    {
                        Console.WriteLine("\tproject: {0}", pr.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Жадная загрузка данных с фильтраций
        /// </summary>
        private void TryEagerLoading2()
        {
            using (var db = new EbookDataContext())
            {
                var options = new DataLoadOptions();
                options.AssociateWith<UserDataModel>(u => u.ProjectDataModels.Where(p => p.ID < 100));
                db.LoadOptions = options;

                Console.WriteLine("-------------------------");
                var users = db.UserDataModels;
                foreach (var user in users)
                {
                    Console.WriteLine("user: {0}", user.Name);
                    foreach (var pr in user.ProjectDataModels)
                    {
                        Console.WriteLine("\tproject: {0}", pr.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Выводим сколько каждый пользователь создал проектов
        /// </summary>
        private void TryCount()
        {
            using (var db = new EbookDataContext())
            {
                var userProjects = from u in db.UserDataModels
                                    select new {u.ID, ProjectsCount = u.ProjectDataModels.Count()};
                                    //from pr in db.ProjectDataModels
                                    //join u in db.UserDataModels on pr.UserID equals u.ID
                                    //where u.Name == "new_user"
                                    //group pr by pr.UserID
                                    //into g
                                    //orderby g.Key
                                    //select g;
                db.Log = new DebugTextWriter();
                foreach (var project in userProjects)
                {
                   // Console.WriteLine("user: {0} projects count: {1}", project.Key, project.Count());
                    Console.WriteLine("user: {0} projects count: {1}", project.ID, project.ProjectsCount);
                }
            }
        }

        /// <summary>
        /// Выводим ограниченное количество записей
        /// </summary>
        private void TryPaging()
        {
            using (var db = new EbookDataContext())
            {
                var projects = (from pr in db.ProjectDataModels
                               select new { pr.ID, pr.Name} )
                               .Skip(1)
                               .Take(2);
                foreach (var project in projects)
                    Console.WriteLine("project id: {0}", project.ID);
            }
        }
        
        /// <summary>
        /// Связывание 2 таблиц и вывод проектов, созданные определенным пользователем 
        /// </summary>
        private void TryJoin()
        {
            using (var db = new EbookDataContext())
            {
                var projects = from pr in db.ProjectDataModels
                               join u in db.UserDataModels on pr.UserID equals u.ID
                               where u.Name == "new_user"
                               //where pr.UserDataModel.Name == "new_user"
                               //.DefaultIfEmpty()
                                select pr;
                //var projects = db.ProjectDataModels.Join(db.UserDataModels, p => p.UserID, u => u.ID, (p, u) => p);
                db.Log = new DebugTextWriter();
                foreach (var project in projects)
                {
                    Console.WriteLine("project: {0}", project.ID);
                }
            }
        }

        /// <summary>
        /// Создаем одновременно 2 зависимых записи в базе данных
        /// </summary>
        private void TryTwoRecordsInsert()
        {
            using (var db = new EbookDataContext())
            {
                var user = new UserDataModel {Name = "new_user", IsActive = true, Password = "1", RoleID = 1};
                //db.Log = new DebugTextWriter();
                var project = new ProjectDataModel
                {
                    Name = "new_user_project",
                    RootPath = "r",
                    StatusID = 1,
                    //UserID = user.ID
                };
                //user.ProjectDataModels.Add(project);
                db.UserDataModels.InsertOnSubmit(user);
                db.ProjectDataModels.InsertOnSubmit(project);
                db.SubmitChanges();
            }
        }
        
        /// <summary>
        /// Выполнение произвольного SQL запроса
        /// </summary>
        private void TryExecuteSql()
        {
            using (var db = new EbookDataContext())
            {
                //var rowsAffected = db.ExecuteCommand("UPDATE Projects SET StatusID = {0} WHERE UserID = {1}", 2, 1);
                //Console.WriteLine("rowsAffected: {0}", rowsAffected);
                var projects = db.ExecuteQuery<ProjectDataModel>("SELECT * FROM Projects");
                foreach (var project in projects)
                    Console.WriteLine("Project: {0}", project.Name);
                
            }
        }

        /// <summary>
        /// Выполнение хранимой процедуры
        /// </summary>
        private void TryCallSP()
        {
            using (var db = new EbookDataContext())
            {
                var results = db.SearchProjects(null, "nname", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                foreach (var result in results)
                {
                    Console.WriteLine("Project: {0}", result.ProjectName);
                }
            }
        }

        /// <summary>
        /// Создаем новую запись: проект
        /// </summary>
        private void TryCreate()
        {
            var project = new ProjectDataModel();
            using (var db = new EbookDataContext())
            {
                project.Name = "NewName1";
                project.StatusID = 1;
                project.UserID = 1;
                project.RootPath = "rootpath";
                //[CreationDate] has default value
                db.ProjectDataModels.InsertOnSubmit(project);
                db.SubmitChanges();
                Console.Write("Project {0} in {1}", project.ID, project.CreationDate);
            }
        }

        /// <summary>
        /// Обновляем значение поля в проекте
        /// </summary>
        private void TryUpdate()
        {
            var projectId = 54;
            using (var db = new EbookDataContext())
            {
                var project = db.ProjectDataModels.SingleOrDefault(p => p.ID == projectId);
                if (project == null)
                    throw new Exception("Проект не найден.");
                project.Name = "";
                project.StatusID = 1;
                //project.CreationDate = DateTime.Now; throw Exception
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Обновление записи без предварительной выборки
        /// </summary>
        private void TryUpdateAlone()
        {
            var project = new ProjectDataModel() {ID = 53};
            using (var db = new EbookDataContext())
            {
                db.ProjectDataModels.Attach(project);
                project.Name = "UpdatedName2";
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Массовой обновление записей
        /// </summary>
        private void TryUpdateAll()
        {
            using (var db = new EbookDataContext())
            {
                var projects = db.ProjectDataModels.Where(p => p.UserID == 1);
                foreach (var project in projects)
                {
                    project.Name = "NName" + project.ID;
                }
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Удаление записи по Id
        /// </summary>
        private void TryDelete()
        {
            var projectId = 54;
            using (var db = new EbookDataContext())
            {
                var project = db.ProjectDataModels.Single(p => p.ID == projectId);
                db.ProjectDataModels.DeleteOnSubmit(project);
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Удаление записи без предварительной выборки
        /// </summary>
        private void TryDeleteAlone()
        {
            var project = new ProjectDataModel() { ID = 55 };
            using (var db = new EbookDataContext())
            {
                db.ProjectDataModels.Attach(project, false);
                db.ProjectDataModels.DeleteOnSubmit(project);
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Массовое удаление записей
        /// </summary>
        private void TryDeleteAll()
        {
            using (var db = new EbookDataContext())
            {
                var projects = db.ProjectDataModels.Where(p => p.StatusID == 2);
                db.ProjectDataModels.DeleteAllOnSubmit(projects);
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Показать текст и тип LINQ команды, а также информацию о соединении
        /// </summary>
        private void TryGetCommand()
        {
            using (var db = new EbookDataContext())
            {
                var projectQuery = from pr in db.ProjectDataModels select pr;
                var dc = db.GetCommand(projectQuery);
                Console.WriteLine("\nCommand Text: \n{0}", dc.CommandText);
                Console.WriteLine("\nCommand Type: {0}", dc.CommandType);
                Console.WriteLine("\nConnection: {0}", dc.Connection);
            }
        }

        /// <summary>
        /// Показать, какие CRUD команды будут выполнены при Submit
        /// </summary>
        private void TryChangeSet()
        {
            using (var db = new EbookDataContext())
            {
                var projectQuery = from pr in db.ProjectDataModels where pr.ID == 51 select pr;
                foreach (var project in projectQuery)
                {
                    Console.WriteLine("ProjectID: {0}", project.ID);
                    Console.WriteLine("\tOriginal value: {0}", project.Name);
                    project.Name = "newProjectName3";
                    Console.WriteLine("\tUpdated value: {0}", project.Name);
                }
                var changeSet = db.GetChangeSet();
                Console.WriteLine("Total changes: {0}", changeSet);
                foreach (var inserted in changeSet.Inserts)
                    Console.WriteLine("Inserted: {0}", inserted);
                foreach (var updated in changeSet.Updates)
                    Console.WriteLine("Updated: {0}", updated);
                foreach (var deleted in changeSet.Deletes)
                    Console.WriteLine("Deleted: {0}", deleted);
                db.SubmitChanges();
            }
        }

        /// <summary>
        /// Вывести в консоль LINQ команды в виде SQL
        /// </summary> 
        private void TryLog()
        {
            using (var db = new EbookDataContext())
            {
#if DEBUG
                db.Log = new DebugTextWriter();
#endif
                var userQuery = from user in db.UserDataModels select user;
                foreach (var user in userQuery)
                {
                    Console.WriteLine("UserID: {0}", user.ID);
                    //Lazy Load
                    //Console.WriteLine("UserID: {0} RoleID: {1} Name: {2}", user.ID, user.RoleID, user.Name);
                }
            }
        }
    }
}
