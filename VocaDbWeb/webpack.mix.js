const mix = require('laravel-mix');

/*
 |--------------------------------------------------------------------------
 | Mix Asset Management
 |--------------------------------------------------------------------------
 |
 | Mix provides a clean, fluent API for defining some Webpack build steps
 | for your Laravel application. By default, we are compiling the Sass
 | file for the application as well as bundling up all the JS files.
 |
 */

require('laravel-mix-merge-manifest');

mix
	.mergeManifest()
	.setPublicPath('./')
	.webpackConfig({
		output: {
			library: 'app'
		}
	})


	/*.scripts([
		"Scripts/jquery-2.2.1.js",
		"Scripts/bootstrap.js",
		//"Scripts/jquery-ui-1.10.1.js", // doesn't work if bundled together
		"Scripts/knockout-3.4.1.js",
		"Scripts/knockout.punches.min.js",
		"Scripts/lodash.js",
		"Scripts/qTip/jquery.qtip.js",
		"Scripts/marked.js"
	], "bundles/shared/libs.js")

	.scripts([
		"Scripts/jquery-ui-1.10.4.js"
	], "bundles/shared/jqui.js")*/
	.js("Scripts/libs.js", "bundles/shared")

	// SHARED BUNDLES
	// Legacy common scripts - should be phased out
	.scripts(["Scripts/VocaDB.js"], "bundles/VocaDB.js")

	.ts("Scripts/App.ts", "bundles")

	// Included on all pages (including front page)
	// Generally the references go from viewmodels -> repositories -> models -> support classes
	.scripts([
	], "bundles/shared/common.js")

	// Included on all pages except the front page (to optimize front page load time).
	.scripts([
		"Scripts/moment-with-locales.js",
	], "bundles/shared/main.js")

	// Included on all entry edit and create pages (album, artist, my settings etc.)
	.scripts([
		"Scripts/knockout-sortable.js"
	], "bundles/shared/edit.js")

	.scripts([
		"Scripts/jquery.tools.min.js"	// REVIEW
	], "bundles/Home/Index.js")

	.scripts([
		"Scripts/jqwidgets27/jqxcore.js", "Scripts/jqwidgets27/jqxrating.js"
	], "bundles/jqxRating.js")


	// VIEW-SPECIFIC BUNDLES
	.scripts([
	], "bundles/ActivityEntry/Index.js")


	// HACK: this produces an empty file called Create.js to prevent 404 errors.
	// TODO: these scripts commands must be removed along with the corresponding RenderScripts in .cshtml files.
	.scripts([
	], "bundles/Album/Create.js")

	.scripts([
	], "bundles/Album/Details.js")

	.scripts([
	], "bundles/Album/Edit.js")

	.scripts([
	], "bundles/Album/Merge.js")

	.scripts([
	], "bundles/Artist/Create.js")

	.scripts([
		"Scripts/soundcloud-api.js"	// REVIEW
	], "bundles/Artist/Details.js")

	.scripts([
	], "bundles/Artist/Edit.js")

	.scripts([
	], "bundles/Artist/Merge.js")

	.scripts([
		"Scripts/page.js"
	], "bundles/Discussion/Index.js")

	.scripts([
	], "bundles/EventSeries/Details.js")

	.scripts([
	], "bundles/EventSeries/Edit.js")

	.scripts([
	], "bundles/ReleaseEvent/Details.js")

	.scripts([
	], "bundles/ReleaseEvent/Edit.js")

	.scripts([
		"Scripts/soundcloud-api.js"	// REVIEW
	], "bundles/Search/Index.js")

	.scripts([
	], "bundles/Song/Create.js")

	.scripts([
		"Scripts/MediaElement/mediaelement-and-player.min.js",
	], "bundles/Song/Details.js")

	.scripts([
	], "bundles/Song/Edit.js")

	.scripts([
	], "bundles/Song/Merge.js")

	.scripts([
		"Scripts/url.js"
	], "bundles/Song/TopRated.js")

	.scripts([
		"Scripts/soundcloud-api.js"	// REVIEW
	], "bundles/SongList/Details.js")

	.scripts([
	], "bundles/SongList/Edit.js")

	.scripts([
	], "bundles/SongList/Featured.js")

	.scripts([
	], "bundles/SongList/Import.js")

	.scripts([
	], "bundles/Tag/Details.js")

	.scripts([
	], "bundles/Tag/Edit.js")

	.scripts([
	], "bundles/Tag/Index.js")

	.scripts([
	], "bundles/Tag/Merge.js")

	.scripts([
	], "bundles/User/AlbumCollection.js")

	.scripts([
		"Scripts/soundcloud-api.js"	// REVIEW
	], "bundles/User/Details.js")

	.scripts([
	], "bundles/User/Index.js")

	.scripts([
	], "bundles/User/Messages.js")

	.scripts([
	], "bundles/User/MySettings.js")

	.scripts([
		"Scripts/soundcloud-api.js"	// REVIEW
	], "bundles/User/RatedSongs.js")

	.scripts([
	], "bundles/Venue/Details.js")

	.scripts([
	], "bundles/Venue/Edit.js");


if (mix.inProduction()) {
	mix.scripts([], "bundles/tests.js");
} else {
	mix.ts("Scripts/tests.ts", "bundles");
}


if (mix.inProduction()) {
	mix.version();
}