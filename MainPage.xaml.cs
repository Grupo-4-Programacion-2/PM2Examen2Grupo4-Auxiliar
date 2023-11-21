﻿using CommunityToolkit.Maui.Views;
using Plugin.AudioRecorder;
using Plugin.Maui.Audio;
using Plugin.Maui.AudioRecorder.Abstractions;
using PM2Examen2Grupo4.Models;
using System;
using System.Diagnostics.CodeAnalysis;

namespace PM2Examen2Grupo4
{

    public partial class MainPage : ContentPage
    {
        Sitios sitios;

        private AudioRecorderService audioRecorderService = new AudioRecorderService()
        {
            StopRecordingOnSilence = false,
            StopRecordingAfterTimeout = false
        };

        readonly IAudioManager _audioManager;
        readonly Plugin.AudioRecorder.AudioPlayer audioPlayer = new Plugin.AudioRecorder.AudioPlayer();

        private bool isRecording = false;
        private bool isPaused = false;
        private MediaElement mediaElement;

        public string pathaudio, filename;
        public MainPage(IAudioManager audioManager)
        {
            InitializeComponent();
            _audioManager = audioManager;
            this.Appearing += OnPageAppearing;

            detener.IsEnabled = false;
        }

        protected void OnAppearing()
        {
            base.OnAppearing();
            VerificarUbicacion();
        }

        private async void OnPageAppearing(object sender, EventArgs e)
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            //Validar para colocar la geolocalizacion 
            if (status == PermissionStatus.Granted)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Default);
                var location = await Geolocation.GetLocationAsync(request);

                try
                {
                    if (location != null)
                    {
                        double latitude = location.Latitude;
                        double longitude = location.Longitude;

                        // Asignar los valores a los campos Entry
                        _lat.Text = latitude.ToString();
                        _lgn.Text = longitude.ToString();
                    }
                    else
                    {
                        // Manejar el caso en el que no se pudo obtener la ubicación
                        await DisplayAlert("Aviso", "No se pudo obtener la ubicación actual.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    // Manejar excepciones, por ejemplo, permisos denegados
                    await DisplayAlert($"Aviso", "Error al obtener la ubicación.", "OK");
                }
            }

            //Validar que tenga conexión a internet
            var current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
            {
                // Hay conexión a Internet
                await DisplayAlert($"Aviso", "Hay conexión a Internet.", "OK");
            }
            else
            {
                // No hay conexión a Internet
                await DisplayAlert($"Error", "No hay conexión a Internet.", "OK");
            }
        }

        private void CheckInternetConnection()
        {
            var current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
            {
                // Hay conexión a Internet
                DisplayAlert("Conexión", "Hay conexión a Internet.", "OK");
            }
            else
            {
                // No hay conexión a Internet
                DisplayAlert("Conexión", "No hay conexión a Internet.", "OK");
            }
        }

        private async void VerificarUbicacion()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    // Permiso de ubicación no concedido, solicitar permiso
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (status == PermissionStatus.Granted)
                    {
                        // Permiso concedido, pero el servicio de ubicación podría estar desactivado
                        var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(1)));

                        if (location == null || (location.Latitude == 0 && location.Longitude == 0))
                        {
                            // El servicio de ubicación no está activado o la ubicación no es válida, mostrar mensaje de validación
                            await DisplayAlert("Aviso", "El servicio de ubicación está desactivado o no se pudo obtener una ubicación válida. Actívalo en la configuración del dispositivo.", "OK");
                        }
                    }
                    else
                    {
                        // Permiso de ubicación no concedido, mostrar mensaje de validación
                        await DisplayAlert("Aviso", "El permiso de ubicación es necesario para acceder a la ubicación.", "OK");
                    }
                }
                else
                {
                    // Permiso ya concedido, verificar si el servicio de ubicación está activado
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(1)));

                    if (location == null)
                    {
                        // El servicio de ubicación no está activado o la ubicación no está disponible, mostrar mensaje de validación
                        await DisplayAlert("Aviso", "El servicio de ubicación está desactivado o no se pudo obtener una ubicación válida. Actívalo en la configuración del dispositivo.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar la ubicación: {ex.Message}");
                // Manejar el error según tus necesidades
            }
        }

        private async void btnGuardar_Clicked(object sender, EventArgs e)
        {
            byte[] imagenBytes = await getSignatureToImage();

            // Validar que la firma no esté vacía
            if (imagenBytes == null || imagenBytes.Length <= 0)
            {
                await DisplayAlert("Aviso", "El área de firma no puede estar vacía. Realiza una firma antes de enviar.", "OK");
                return;
            }

            // Validar que los campos de ubicación y descripción no estén vacíos
            if (string.IsNullOrWhiteSpace(_lat.Text) || string.IsNullOrWhiteSpace(_lgn.Text) || string.IsNullOrWhiteSpace(_des.Text))
            {
                await DisplayAlert("Aviso", "Los campos de ubicación y descripción no pueden estar vacíos. Por favor, completa la información antes de grabar.", "OK");
                return;
            }

            sitios = new Sitios
            {
                descripcion = _des.Text,
                latitud = Convert.ToDouble(_lat.Text),
                longitud = Convert.ToDouble(_lgn.Text),
                audioFile = pathaudio,
                firmaDigital = imagenBytes
            };


            Console.WriteLine(sitios.descripcion);
            Console.WriteLine(sitios.longitud);
            Console.WriteLine(sitios.latitud);
            Console.WriteLine(sitios.audioFile);
            Models.Msg msg = await Controllers.SitiosController.CreateEmple(sitios);


            if (msg != null)
            {
                await DisplayAlert("Aviso", msg.message.ToString(), "OK");
                clear();
            }
        }

        private async void btnLista_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Views.PageList());
        }

        private async void recording_Clicked(object sender, EventArgs e)
        {

            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                return;
            }

            if (!audioRecorderService.IsRecording)
            {
                string nombre = DateTime.Now.ToString("ddMMyyyymmss") + "_VoiceNote.wav";
                audioRecorderService.FilePath = "/storage/emulated/0/Android/data/com.companyname.pm2Examen2Grupo4/files/Audio/" + nombre;
                await audioRecorderService.StartRecording();
                detener.IsEnabled = true;
                recording.IsEnabled = false;
            }
        }


        private async Task<byte[]> getSignatureToImage()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DrawingView drawingView = this.FindByName<DrawingView>("drawingView");

                if (drawingView.Lines.Count > 0)
                {
                    Stream imagenStream = await ((DrawingView)this.FindByName<DrawingView>("drawingView")).GetImageStream(200, 200);
                    await imagenStream.CopyToAsync(stream);
                    return stream.ToArray();
                }
                else
                {
                    return null;
                }

            }
        }

        private async void detener_Clicked(object sender, EventArgs e)
        {
            await audioRecorderService.StopRecording();

            audioPlayer.Play(audioRecorderService.GetAudioFilePath());
            pathaudio = audioRecorderService.GetAudioFilePath();
            Console.WriteLine("Deteniendo grabación y guardando el audio...");

            detener.IsEnabled = false;
            recording.IsEnabled = true;

        }

        private void clear()
        {
            drawingView.Clear();
            _des.Text = "";
        }

    }
}