<%@ Application ClassName="Mono.Website.Global" %>
<%@ Import Namespace="Monodoc" %>
<%@ Assembly name="monodoc" %>

<script runat="server" language="c#" >
public static RootTree help_tree;

void Application_Start ()
{
	HelpSource.use_css = true;
	HelpSource.FullHtml = false;
	HelpSource.UseWebdocCache = true;
	help_tree = RootTree.LoadTree ();
	SettingsHandler.Settings.EnableEditing = false;
}

</script>
