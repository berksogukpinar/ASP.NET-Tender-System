var json,seconds;
async function veri() {
    let response = await fetch("http://localhost:55612/Home/GetClockData");
            json = await response.json();
            console.log(json)
            seconds = json.TotalSeconds | 0;
}
veri();


 
function timer() {
    var days = Math.floor(seconds / 24 / 60 / 60);
    var hoursLeft = Math.floor((seconds) - (days * 86400));
    var hours = Math.floor(hoursLeft / 3600);
    var minutesLeft = Math.floor((hoursLeft) - (hours * 3600));
    var minutes = Math.floor(minutesLeft / 60);
    var remainingSeconds = seconds % 60;
    function pad(n) {
        return (n < 10 ? "0" + n : n);
    }
   
        document.getElementById('countdown').innerHTML = pad(days) + " gün " + pad(hours) + " saat " + pad(minutes) + " dakika " + pad(remainingSeconds) + " saniye ";
    
    if (seconds == 0) {
        clearInterval(countdownTimer);
        document.getElementById('countdown').innerHTML = "Tamamlandı!";
    } else {
        seconds--;
    }
}
var countdownTimer = setInterval('timer()', 1000);


$(document).ready(function () {
    // Test for placeholder support
    $.support.placeholder = (function () {
        var i = document.createElement('input');
        return 'placeholder' in i;
    })();

    // Hide labels by default if placeholders are supported
    if ($.support.placeholder) {
        $('.form-label').each(function () {
            $(this).addClass('js-hide-label');
        });

        // Code for adding/removing classes here
        $('.form-group').find('input, textarea').on('keyup blur focus', function (e) {

            // Cache our selectors
            var $this = $(this),
                $label = $this.parent().find("label");

            switch (e.type) {
                case 'keyup': {
                    $label.toggleClass('js-hide-label', $this.val() == '');
                } break;
                case 'blur': {
                    if ($this.val() == '') {
                        $label.addClass('js-hide-label');
                    } else {
                        $label.removeClass('js-hide-label').addClass('js-unhighlight-label');
                    }
                } break;
                case 'focus': {
                    if ($this.val() !== '') {
                        $label.removeClass('js-unhighlight-label');
                    }
                } break;
                default: break;
            }
            // previous implementation with ifs
            /*if (e.type == 'keyup') {
                if( $this.val() == '' ) {
                    $parent.addClass('js-hide-label'); 
                } else {
                    $parent.removeClass('js-hide-label');   
                }                     
            } 
            else if (e.type == 'blur') {
                if( $this.val() == '' ) {
                    $parent.addClass('js-hide-label');
                } 
                else {
                    $parent.removeClass('js-hide-label').addClass('js-unhighlight-label');
                }
            } 
            else if (e.type == 'focus') {
                if( $this.val() !== '' ) {
                    $parent.removeClass('js-unhighlight-label');
                }
            }*/
        });
    }
});

