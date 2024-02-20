using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Czogala4FLos
{
    public partial class MainPage : ContentPage
    {
        private const string ClassesFileName = "classes.txt";
        private Dictionary<string, List<string>> classes = new Dictionary<string, List<string>>();
        private Dictionary<string, int> unavailableStudents = new Dictionary<string, int>();
        private const int RoundsToExclude = 4; //4-1 immunitet na 3 losowania

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadClasses();
        }

        private async void LoadClasses()
        {
            try
            {
                if (File.Exists(FileSystem.AppDataDirectory + "/" + ClassesFileName))
                {
                    var lines = await File.ReadAllLinesAsync(FileSystem.AppDataDirectory + "/" + ClassesFileName);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(';');
                        classes[parts[0]] = parts.Skip(1).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Nie udało się załadować klas: {ex.Message}", "OK");
            }
        }

        private async void AddClass_Clicked(object sender, EventArgs e)
        {
            string className = await DisplayPromptAsync("Dodaj klasę", "Wpisz nazwę klasy:");
            if (!string.IsNullOrWhiteSpace(className))
            {
                string studentsInput = await DisplayPromptAsync("Dodaj klasę", "Wpisz imiona uczniów, oddzielając je przecinkami:");
                var students = studentsInput.Split(',').Select(s => s.Trim()).ToList();
                classes[className] = students;
                await SaveClasses();
            }
        }

        private async void EditClass_Clicked(object sender, EventArgs e)
        {
            string selectedClass = await DisplayActionSheet("Edytuj klasę", "Anuluj", null, classes.Keys.ToArray());
            if (selectedClass != "Anuluj")
            {
                string newClassName = await DisplayPromptAsync("Edytuj klasę", "Wpisz nową nazwę klasy:", initialValue: selectedClass);
                string studentsInput = await DisplayPromptAsync("Edytuj klasę", "Wpisz nową listę uczniów, oddzielając imiona przecinkami:", initialValue: string.Join(", ", classes[selectedClass]));
                if (!string.IsNullOrWhiteSpace(newClassName) && !string.IsNullOrWhiteSpace(studentsInput))
                {
                    var students = studentsInput.Split(',').Select(s => s.Trim()).ToList();
                    classes.Remove(selectedClass);
                    classes[newClassName] = students;
                    await SaveClasses();
                }
            }
        }

        private async void DeleteClass_Clicked(object sender, EventArgs e)
        {
            string selectedClass = await DisplayActionSheet("Usuń klasę", "Anuluj", null, classes.Keys.ToArray());
            if (selectedClass != "Anuluj")
            {
                classes.Remove(selectedClass);
                await SaveClasses();
            }
        }

        private async void PickClass_Clicked(object sender, EventArgs e)
        {
            string selectedClass = await DisplayActionSheet("Wybierz klasę", "Anuluj", null, classes.Keys.ToArray());
            if (selectedClass != "Anuluj")
            {
                var students = classes[selectedClass].Where(s => !unavailableStudents.ContainsKey(s)).ToList();

                if (students.Count == 0)
                {
                    await DisplayAlert("Uwaga", "Wszyscy uczniowie zostali już wylosowani do odpowiedzi.", "OK");
                    return;
                }

                Random random = new Random();
                string pickedStudent = students[random.Next(students.Count)];

                if (unavailableStudents.ContainsKey(pickedStudent))
                    unavailableStudents[pickedStudent] = RoundsToExclude;
                else
                    unavailableStudents.Add(pickedStudent, RoundsToExclude);

                UpdateUnavailableStudents();

                pickedStudentLabel.Text = "Wylosowany uczeń: " + pickedStudent;
            }
        }

        private void UpdateUnavailableStudents()
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in unavailableStudents)
            {
                if (kvp.Value > 1)
                    unavailableStudents[kvp.Key]--;
                else
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                unavailableStudents.Remove(key);
            }
        }

        private async Task SaveClasses()
        {
            try
            {
                var lines = classes.Select(kvp => kvp.Key + ";" + string.Join(";", kvp.Value));
                await File.WriteAllLinesAsync(FileSystem.AppDataDirectory + "/" + ClassesFileName, lines);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Nie udało się zapisać klas: {ex.Message}", "OK");
            }
        }
    }
}
