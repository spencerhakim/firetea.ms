$(document).ready(function(){
    'use strict';

    //console shim
    (function(){
        var f = function(){};
        if( !window.console )
            window.console = { log:f, info:f, warn:f, debug:f, error:f };
    })();

    //make sure we're on the right page (and there aren't any server errors)
    if( window.location.pathname !== '/' || $('#main').length === 0)
        return;

    var eyesup = new buzz.sound('/Content/eyesup', {
        formats: [ 'ogg', 'mp3' ]
    });

    //#region Alert functions
    function displayMsg(type, msg)
    {
        $('#alert')
            .removeClass(function(index, css){
                return (css.match (/(^|\s)alert-\S+/g) || []).join(' ');
            })
            .addClass('alert-' + type)
            .html(msg)
            .show();
    }

    var $updateInfo = $('#updateInfo');
    if( ($.localStorage.get('closedUpdateInfo') || 0) < $updateInfo.data('timestamp') )
        $updateInfo.show();

    $updateInfo.on('closed.bs.alert', function(){
        $.localStorage.set('closedUpdateInfo', Date.now());
    });
    //#endregion

    //#region Helper functions
    var registered = false;
    function getParty()
    {
        return {
            Region: $('#region-select').val(),
            Platform: $('#platform-select').val(),
            Activity: $('#activity-select').val(),
            PartySize: $('#partysize-input').val(),

            Username: $('#username-input').val(),
            Level: $('#level-input').val()
        };
    }

    function updateTitle(text)
    {
        if( text != undefined && text != '' )
            document.title = text + ' • Firetea.ms';
        else
            document.title = 'Firetea.ms';
    }

    function setStep(step)
    {
        updateTitle();
        $('#main')
            .removeClass(function(index, css){
                return (css.match (/(^|\s)step\S+/g) || []).join(' ');
            })
            .addClass('step' + step);
    }

    function createResult(user)
    {
        var _class = 'guardianclassless';

        //for DestinyRep links
        var platform = -1;
        switch( user.Platform )
        {
            case 0:
            case 1:
                platform = 2;
                break;

            case 2:
            case 3:
                platform = 1;
                break;
        }
        var repLink = 'http://www.destinyrep.com/#un=' + encodeURIComponent(user.Username) + '&t=' + platform;

        var $div =
            $('<div>' +
                '<span class="name"><a target="_blank"></a></span>' +
                '<span class="size"></span>' +
                '<span class="level"></span>' +
            '</div>')
           .addClass(_class);

        $div.find('.name a')
            .attr('href', encodeURI(repLink))
            .text(user.Username);

        if( user.PartySize > 1 )
            $div.find('.size')
                .text('(x' + user.PartySize + ')');

        $div.find('.level')
            .text(user.Level);

        return $div;
    }

    function regSuccess()
    {
        registered = true;
        $('#alert.alert-danger').hide();

        mm.server.getTimeToMatch()
            .done(function(avgttm){
                $('#avgttm').text(avgttm);
            });
        setStep(2);
    }

    function regFail()
    {
        registered = false;
        $('#searchbtn')
            .prop("disabled", false)
            .text('Search');
        setStep(1);
    }

    function register()
    {
        mm.server.register(getParty())
            .done(function(success){
                if( success )
                    regSuccess();
                else
                    regFail();
            })
            .fail(function(error){
                mm.client.DisplayError('An error occured, please try again.');
                console.error(error);
                regFail();
            });
    }

    function startConnection()
    {
        $.connection.hub.start()
            .done(function(){
                $('#searchbtn').prop('disabled', false);
                console.log('Now connected, ConnectionID=' + $.connection.hub.id);
            })
            .fail(function(error){
                mm.client.DisplayError('Could not connect to server, retrying&hellip;');
                console.error(error);
                setTimeout(startConnection, 15000);
            });
    }
    //#endregion

    //#region SignalR client stuff
    var mm = $.connection.v1;
    mm.client.DisplayInfo = function(msg){
        displayMsg('info', msg);
    };

    mm.client.DisplayWarning = function(msg){
        displayMsg('warning', msg);
    };

    mm.client.DisplayError = function(msg){
        displayMsg('danger', msg);
    };

    mm.client.UpdateStatus = function(text){
        $('#searchstatus').html(text + '&hellip;');
    };

    mm.client.UpdateProgress = function(current, needed){
        var progress = current + '/' + needed;
        $('#searchprogress').text(progress);
        updateTitle(progress);
    };

    mm.client.MatchFound = function(users){
        registered = false;
        $.connection.hub.stop();

        users.forEach(function(user){
            $('#mmresults2').append(createResult(user));
        });
        $('#alert').hide();
        setStep(3);
        eyesup.play();
    };
    //#endregion

    //#region SignalR hub stuff
    var tryingToReconnect = false;
    $.connection.hub.qs = {
        'client' : 'web',
        'version' : _deployId
    };

    $.connection.hub.connectionSlow(function(){
        mm.client.DisplayWarning('Detecting an issue with the connection, this may affect your ability to find a match.');
        console.warn('ConnectionSlow - ' + $.connection.hub.lastError);
    });

    $.connection.hub.reconnecting(function(){
        tryingToReconnect = true;
        mm.client.DisplayError('Lost connection to server, attempting to reconnect&hellip;');
    });

    $.connection.hub.reconnected(function(){
        $('#searchbtn').prop('disabled', false);
        if( !tryingToReconnect )
            return;

        tryingToReconnect = false;
        mm.client.DisplayInfo('Reconnected to server.');
        if( registered )
            register();
    });

    $.connection.hub.disconnected(function(){
        $('#searchbtn').prop('disabled', true);

        if( tryingToReconnect )
        {
            console.error('Disconnected - ' + $.connection.hub.lastError);
            setTimeout(startConnection, 5000);
            regFail();
        }
    });

    startConnection();
    //#endregion

    //#region Form stuff
    $('input, select').phoenix();
    $('#partysize-input').val(1);

    $('#mmform').submit(function(e){
        e.preventDefault();

        $('#searchbtn')
            .prop("disabled", true)
            .html('Connecting&hellip;');

        register();
    });
    //#endregion

});