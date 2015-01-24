//var gSiteUrl;

angular.module('link', []).
	  directive('activeLink', ['$location', function (location)
	  {
	  	return {
	  		restrict: 'A',
	  		link: function (scope, element, attrs, controller)
	  		{
	  			var clazz = attrs.activeLink;
	  			var path = attrs.href;
	  			
	  			if (path === window.location.pathname)
	  			{
	  				element.addClass(clazz);
	  			} else
	  			{
	  				element.removeClass(clazz);
	  			}
	  		}
	  	};
	  }]);

$( document ).ready(function() 
{
	$(document).submit( function(event) 
	{
		event.stopPropagation();

		var form = $(event.target);
		var ajax = $(form).attr('ajax');
		if ($(form).valid() && ajax == "true")
		{
			$.ajax({	type: "POST",  
						url: $(form).attr('action'),  
						data: $(form).serialize(),  
						success:function(data)
								{
									RouteToHandler( $(form).attr("handler"), data, form);
								}});

			$('input[type=submit]', $(form)).attr('disabled', 'disabled');

			return false;
		}
	});
	
	//gSiteUrl = $('meta[name=site_url]').attr("content");

	
	// call page specific OnLoad function
	if (typeof (OnLoad) != "undefined")
	{
		OnLoad();
	}
});

/**
*/
function RouteToHandler(handlerName, data, form)
{
	window[handlerName].apply(null, [data]);
	$('input[type=submit]', $(form)).removeAttr('disabled');
}

/**
 * 
 */
function HtmlEncode(value)
{
  //create a in-memory div, set it's inner text(which jQuery automatically encodes)
  //then grab the encoded contents back out.  The div never exists on the page.
  return $('<div/>').text(value).html();
}