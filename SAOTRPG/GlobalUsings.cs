// Terminal.Gui 2.4.5 splits its API across sub-namespaces (Views, ViewBase, Drawing, App, Input...).
// Absorbing them here keeps bare type names working across the whole project.
// The Attribute alias resolves the collision with System.Attribute in favor of Terminal.Gui's.
global using Terminal.Gui.App;
global using Terminal.Gui.Configuration;
global using Terminal.Gui.Drawing;
global using Terminal.Gui.Input;
global using Terminal.Gui.Text;
global using Terminal.Gui.ViewBase;
global using Terminal.Gui.Views;
global using Attribute = Terminal.Gui.Drawing.Attribute;
global using KeyCode = Terminal.Gui.Drivers.KeyCode;
