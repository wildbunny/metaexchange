function PostForm($http, url, paramsObj, onSuccess)
{
	return $http({
		method: 'POST',
		url: url,
		data: $.param(paramsObj),
		headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
	}).success(onSuccess);
}

$( document ).ready(function() 
{
	$(document).submit( function(event) 
	{
		event.stopPropagation();

		var form = $(event.target);
		var ajax = $(form).attr('ajax');
		if (ajax != null)
		{
			if ($(form).valid())
			{
				$.ajax({	type: "POST",  
							url: $(form).attr('action'),  
							data: $(form).serialize(),  
							success:function(data)
									{
										RouteToHandler( $(form).attr("handler"), data, form);
									}});

				$('input[type=submit]', $(form)).attr('disabled', 'disabled');
			}

			return false;
		}
	});
	
	//gSiteUrl = $('meta[name=site_url]').attr("content");
	// override jquery validate plugin defaults
	$.validator.setDefaults({
		highlight: function (element)
		{
			$(element).closest('.form-group').addClass('has-error');
		},
		unhighlight: function (element)
		{
			$(element).closest('.form-group').removeClass('has-error');
		},
		errorElement: 'span',
		errorClass: 'help-block',
		errorPlacement: function (error, element)
		{
			if (element.parent('.input-group').length)
			{
				error.insertAfter(element.parent());
			} else
			{
				error.insertAfter(element);
			}
		}
	});
	
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

Date.prototype.timeNow = function (includeSeconds, hideNonTime)
{
	var m_shortMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
	var seconds = (includeSeconds != undefined) ? ":" + this.format(this.getSeconds()) : "";
	var nonTime = this.getDate() + " " + m_shortMonths[this.getMonth()] + " ";
	if (hideNonTime == true)
	{
		nonTime = "";
	}
	return nonTime + this.format(this.getHours()) + ":" + this.format(this.getMinutes()) + seconds;
};

function renameSymbolPair(name)
{
	return name != undefined ? name.replace("_", "/") : "";
}