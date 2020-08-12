﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Atlas.Client.Components.Shared
{
    public abstract class EditorComponent : SharedComponentBase
    {
        [Parameter] public string Value { get; set; }
        [Parameter] public int Rows { get; set; }
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        protected string Preview { get; set; } = string.Empty;

        protected async Task PreviewAsync()
        {
            if (!string.IsNullOrWhiteSpace(Value))
            {
                Preview = null;
                var response = await ApiService.PostAsJsonAsync("api/public/preview", Value);
                Preview = await response.Content.ReadAsStringAsync();
            }
            else
            {
                Preview = "<p>&nbsp;</p>";
            }
        }

        protected Task OnValueChanged(ChangeEventArgs e)
        {
            Value = e.Value.ToString();
            return ValueChanged.InvokeAsync(Value);
        }
    }
}