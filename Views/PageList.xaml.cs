using CommunityToolkit.Maui.Views;
using Plugin.Maui.Audio;
using PM2Examen2Grupo4.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Plugin.AudioRecorder;

namespace PM2Examen2Grupo4.Views;

public partial class PageList : ContentPage
{

    private readonly IAudioManager audioManager;
    readonly Plugin.AudioRecorder.AudioPlayer audioPlayer = new Plugin.AudioRecorder.AudioPlayer();

    private MediaElement mediaElement;
    byte[] k;
    public PageList()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        List<Models.Sitios> listSitios = new List<Models.Sitios>();
        listSitios = await Controllers.SitiosController.GetSitios();
        _list.ItemsSource = listSitios;

    }

    private async void _list_ItemSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.CurrentSelection[0] as Sitios;


        if (selectedItem != null)
        {
            string action = await DisplayActionSheet("¿Que Quieres Hacer?", "CANCELAR", "", "Actualizar Informacion", "Reproducir Audio", "Ir Mapa", "Eliminar");

            switch (action)
            {
                case "Ir Mapa":
                    await Navigation.PushAsync(new Views.PageMap(selectedItem.latitud, selectedItem.longitud, selectedItem.descripcion));
                    break;

                case "Eliminar":
                    Models.Msg msg = await Controllers.SitiosController.DeleteEmple(selectedItem.id);
                    if (msg != null)
                    {
                        await DisplayAlert("Aviso", msg.message.ToString(), "CONTINUAR");
                    }
                    OnAppearing();
                    break;

                case "Actualizar Informacion":
                    await Navigation.PushAsync(new Views.PageUpdate(selectedItem.latitud, selectedItem.longitud, selectedItem.descripcion, selectedItem.audioFile, selectedItem.id)); //Se pueden mandar los parametros de actualizacion mediante el constructor
                    break;

                case "Reproducir Audio":
                    PlayAudio(selectedItem.audioFile + ".wav");
                    break;
            }
        }
    }

    private async void ToolbarItem_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage(audioManager));
    }

    private void PlayAudio(string audioPath)
    {

        audioPlayer.Play(audioPath);

        Console.WriteLine("Deteniendo grabación y guardando el audio...");

    }

}