/*
    This is a GLOBAL Pegaun script. This script will be shared by all of Pegasun's project.
    Do NOT add app-specific code here. Anything added here, will be added to all other Pegasun apps as well.

    For APP only specific scripts, use the app.js.

    Dependency: refjs.js, alertify.js, bootstrap.native - https://thednp.github.io/bootstrap.native/
*/

var pega = (function ()
{
    'use strict';

    function main() { }

    // Define your global variables inside Settings. Avoid Global namespace polution.
    main.Settings = {
        allowProgressionOnProgressBar: true,
        currentImageProgressBar: null,
        mainModalObject: null,
        secondModalObject: null,
        customEventsArray: {},
        TableKeys: {} // E.g. dict['tableId'] = new List('id'); ... ListJS needed.
    }

    main.SubscribeEmailMailing = subscribeEmailMailing;
    main.ResultWasSuccess = resultWasSuccess;
    main.DisplayResultsItem = displayResultsItem;
    main.ShowMiniBrowserPopupWindow = showMiniBrowserPopupWindow;
    main.UnobtrusiveLoadModal = unobtrusiveLoadModal;
    main.InitializePegaMiniSlider = initializePegaMiniSlider;
    main.AssignProperImageToSlider = assignProperImageToSlider;
    main.HandleMiniSlideChangeEvent = handleMiniSlideChangeEvent;
    main.InitializePageMainSlider = initializePageMainSlider;
    main.ShowSiteWideOverlay = showSiteWideOverlay;
    main.HideSiteWideOverlay = hideSiteWideOverlay;

    //#region Global Functions
    // Subscribe email should be in /Company/SubscribeEmail
    // Can have multiple "pretend" forms.
    // Create a div and have 2 items. A button and an email.
    // give the class 'subscribe-email-text' to textbox, and 'subscribe-email-button' to send button.
    function subscribeEmailMailing(targetId)
    {
        var emailPretendForm = rj.Id(targetId);
        var emailFormTextBox = emailPretendForm.getElementsByClassName('subscribe-email-text')[0];
        var emailFormButton = emailPretendForm.getElementsByClassName('subscribe-email-button')[0];

        var email = emailFormTextBox.value;
        if (email.length < 4 && (!rj.StringContains(email, "@") || !rj.StringContains(email, ".")))
        {
            alertify.error("Please enter a valid email address.");
            return;
        }

        Button(emailFormButton, 'loading');

        rj.AsyncPost(rj.UrlAction('SubscribeEmail', 'Company'), { email: email }, { isJson: true }).then(function (result)
        {
            if (displayResultsItem(result))
            {
                emailPretendForm.style.display = 'none';
            }
            else
            {
                Button(emailFormButton, 'reset');
            }
        }).catch(pega.OnFetchError);
    }

    // on <form> -> onkeypress="(event, submitFunc)",  onsubmit="return false"
    main.SubmitFormOnEnterPressed = function (e, submitFunc)
    {
        if (e && e.keyCode === 13)
        {
            e.preventDefault();
            submitFunc();
        }
    }

    //#endregion Global Functions

    //#region Miscellaneous

    /* ----- Bootstrap.Native -----
        For things you load in partial that are data-toggle dependant, such as "data-toggle='tabs'"
        you can use JS to call these functions after load.
            - var testTab = new Tab(rj.find('#testTab'));
       ---------------------------- */

    function resultWasSuccess(result) { return (result.ResultType == null || result.ResultType === 0 || result.ResultType === 1); }

    function displayResultsItem(result)
    {
        hideSiteWideOverlay();

        // If null, it's just a simple return message.
        if (result.ResultType == null)
        {
            alertify.success(result);
        }
        else if (result.ResultType === 4) // Error
        {
            alertify.error(result.Message);
        }
        else if (result.ResultType === 1) // Simple Info
        {
            alertify.info(result.Message);
        }
        else if (result.ResultType === 0) // Success
        {
            alertify.success(result.Message);
        }

        return resultWasSuccess(result);
    }

    function showMiniBrowserPopupWindow(url)
    {
        window.open(url, 'popUpWindow', 'height=700, width=800, left=10, top=10, resizable=yes, scrollbars=yes, toolbar=yes, menubar=no, location=no, directories=no, status=yes');
    }

    main.OnFetchError = function (ex)
    {
        hideSiteWideOverlay();
        console.error(ex);
        alertify.error(ex.message);
    }

    main.BrowserSupportsAllFeatures = function ()
    {
        return window.Promise && window.fetch;
    }

    // Overlays/Loading
    function showSiteWideOverlay() { rj.Id('siteWideOverlay').style.display = 'block'; }
    function hideSiteWideOverlay() { rj.Id('siteWideOverlay').style.display = 'none'; }

    //#endregion Miscellaneous

    //#region Modal, Tabs, Etc... Bootstrap Native functions ---

    main.ButtonLoading = function (id) { Button(rj.Id(id), 'loading'); }
    main.ButtonReset = function (id) { Button(rj.Id(id), 'reset'); }

    // Define a main modal global -> var MainModalObj = new Modal($("#mainModal"), {});
    // In the future this will be changed, however, currently, this is the best option memory wise.
    // options -> Title, AddCloseButton, Size, HideFooter, allowCloseOnBackground
    main.LoadModal = function (targetId, url, param, options)
    {
        main.ShowSiteWideOverlay();
        return rj.AsyncGet(url, param, options).then(function (result)
        {
            if (rj.StringContains(result, '{"ResultType":'))
            {
                // Probably an error occured. We've got back JSON string. Convert to JSON and display it.
                var resultsObj = JSON.parse(result);
                displayResultsItem(resultsObj);
                return;
            }

            targetId = targetId.replace("#", "");
            var modalObject = rj.Id(targetId);
            var modalBodyObject = modalObject.querySelector('.modal-body');
            var modalFooterObject = modalObject.querySelector('.modal-footer');

            rj.Empty(modalBodyObject);
            modalBodyObject.innerHTML = result;

            // Script fix. innerHTML=<script> does not produce executable script. 
            var modalScripts = modalBodyObject.querySelectorAll("script");
            rj.ForEach(modalScripts, function (scriptNode) { rj.NodeScriptReload(scriptNode); });

            if (options == null || options === 'undefined') { options = {}; }

            // Defines the Title
            if (rj.IsValidValue(options.Title))
            {
                var modalTitleObject = modalObject.querySelector('.modal-title');
                modalTitleObject.innerHTML = options.Title;
            }

            // Hide footer if requested
            rj.Show(modalFooterObject, !(options.HideFooter === true));

            // Automatically Adds a close button if requested
            if (options.HideFooter !== true && options.AddCloseButton === true)
            {
                rj.Empty(modalFooterObject);
                modalFooterObject.innerHTML += '<button class="btn btn-danger btn-sm" data-dismiss="modal">Close</button>';
            }

            // Modal size. Available: max, mid or leave empty for lg (large)
            var modalDialogObj = document.querySelector("#" + targetId + ' .modal-dialog');
            var sizeClass = rj.IsValidValue(options.Size) ? (options.Size === 'max' ? 'pega-relative-bg-modal' : (options.Size === 'mid' ? "" : "modal-lg")) : "modal-lg";
            if (rj.IsValidValue(sizeClass)) { modalDialogObj.classList.add(sizeClass); }

            var allowCloseOnBackgroundClick = options.allowCloseOnBackground === true ? '' : 'static';

            var modalOptions =
                {
                    backdrop: allowCloseOnBackgroundClick,
                    keyboard: false
                }

            if (rj.StringContains("#" + targetId, "secondModal"))
            {
                if (main.Settings.mainModalObject != null) { main.Settings.mainModalObject.hide(); }

                main.Settings.secondModalObject = null;
                main.Settings.secondModalObject = new Modal(modalObject, modalOptions);
                main.Settings.secondModalObject.show();
            }
            else
            {
                main.Settings.mainModalObject = null;
                main.Settings.mainModalObject = new Modal(modalObject, modalOptions);
                main.Settings.mainModalObject.show();
            }
        }).catch(main.OnFetchError).finally(main.HideSiteWideOverlay);
    }

    main.InitializeTabs = function (tabId)
    {
        var myTabs = document.getElementById(tabId);
        var myTabsCollection = myTabs.getElementsByTagName('a');
        for (var i = 0; i < myTabsCollection.length; i++)
        {
            new Tab(myTabsCollection[i], { height: true });
        }
    }

    main.EmptyModal = function (targetId)
    {
        targetId = targetId.replace('#', '');
        rj.Empty(rj.StringFormat('#{0} .modal-title, #{0} .modal-body, #{0} .modal-footer', targetId));
        var modalDialogObj = document.querySelector(rj.StringFormat('#{0} .modal-dialog', targetId));
        modalDialogObj.classList.remove('pega-relative-bg-modal');
        modalDialogObj.classList.remove('modal-lg');
    }

    main.ToggleClickPopover = function (targetId)
    {
        var target = rj.Id(targetId);
        if (rj.IsNullOrUndefined(target.Popover))
        {
            new Popover(target, { trigger: 'click' }).toggle();
            return;
        }
        target.Popover.toggle();
    }

    //#endregion Modal END 

    //#region Unobtrusive Functions

    // Example on how to use below. Just create one instance and add your global listeners there. 
    // document.addEventListener('click', function(e) { if (e.target.hasAttribute('data-loadpartial')) { pega.UnobtrusiveLoadModal(e.target); } });

    /*
        Targets "data-loadmodal". Loads url content into desired target id modal.
        data-url: the url to load the content.
        data-targetid: the target id (of the Modal) to load in to. 
        data-params: the parameters (if any). Must be in string format. E.g. "id=10&lastName=Tester".
        data-success: the name of the success function to call. E.g. "editFormLoadSucceeded".
        
        # Options
        data-title: The title of the Modal to display.
        data-size: Modal size. Available: max, mid or leave empty for lg (large).
        data-closebutton: Adds close button on the footer of the modal.
        data-hidefooter: Hides the footer section of the modal.
    */
    function unobtrusiveLoadModal(target)
    {
        var url = target.getAttribute('data-url');
        var targetId = target.getAttribute('data-targetid');
        var size = target.getAttribute('data-size');
        if (target.hasAttribute('data-params')) { url = url + "?" + target.getAttribute('data-params'); }

        main.LoadModal('#' + targetId, url, null,
        {
            Title: target.getAttribute('data-title'),
            AddCloseButton: target.hasAttribute('data-closebutton'),
            Size: size,
            HideFooter: target.hasAttribute('data-hidefooter')
        }).then(function ()
        {
            var successFuncToCall = rj.GenerateUnobtrusiveFunction(target.getAttribute('data-success'));
            if (rj.IsFunction(successFuncToCall)) { successFuncToCall(); }
        });
    }

    main.UnobtrusiveLoadPartial = function (target)
    {
        pega.ShowSiteWideOverlay();
        rj.UnobtrusiveLoadPartial(target).finally(pega.HideSiteWideOverlay);
    }

    /*
        Put it on <th>
        Targets "data-sorttable". Sorts the table based on the selected column. Automatically handles asc/desc ordering.
        data-sorttable
        data-tablekey: StoredOn pega.Settings.TableKeys (Add it to a global object when new List('id')). E.g. dict['tableId'] = new List('id');
        data-sortvalue: the column we are sorter. "Name", "Profit", etc. Have it in <span class='profit'>50</span>
        data-sortfunction: pass the name of custom sorting function to use. Sort function has 2 parameters. E.g. pass "CustomSort", where actual name function CustomSort(a, b) {}
        inside <th>, if <span data-icon> is specified, it will automatically add up/down arrow icon.
    */
    main.UnobtrusiveListJSSort = function (target)
    {
        if (target.nodeName.toUpperCase() !== "TH") { target = rj.FindParent(target, "th"); }

        var parentTable = rj.FindParent(target, "table");
        var tableLJS = main.Settings.TableKeys[parentTable.id];
        var sortValue = target.getAttribute('data-sortvalue');

        var sortOrder = target.hasAttribute('data-sortdesc') ? "desc" : "asc";
        if (sortOrder === "asc") { target.setAttribute("data-sortdesc", ""); }
        else { target.removeAttribute('data-sortdesc'); }

        // Add up/down arrow icon
        var spanIconObj = target.querySelector("span[data-icon]");
        if (spanIconObj != null && spanIconObj != undefined)
        {
            rj.ForEach(parentTable.getElementsByClassName("ion-arrow-down-b"), function (e) { e.classList.remove("ion-arrow-down-b") });
            rj.ForEach(parentTable.getElementsByClassName("ion-arrow-up-b"), function (e) { e.classList.remove("ion-arrow-up-b") });
            spanIconObj.classList.add((sortOrder == "asc" ? "ion-arrow-down-b" : "ion-arrow-up-b"));
        }

        var customSortFunction = null;
        if (target.hasAttribute("data-sortfunction") && typeof window[target.getAttribute("data-sortfunction")] === 'function')
        {
            customSortFunction = window[target.getAttribute("data-sortfunction")];
        }

        if (customSortFunction != null)
        {
            tableLJS.sort(sortValue, { sortFunction: customSortFunction, order: sortOrder });
            return;
        }

        tableLJS.sort(sortValue, { order: sortOrder });
    }

    //#endregion

    //#region Page Main Slider 
    /*
        Dependency: ref.js
        How to use ---------------------------------
        (See "Main/Index.cshtml" for working example)
        1. Initiate <div id='pageSliderMain'>
        
        2. Inside it, create divs with psm#. For example, <div id='psm1'> , 'psm2', 'psm3' for each slides. 
            2.1. Make sure each 'psm' divs have the class 'container' (This is a must - at the moment)
            2.2. You can assign background image or background color using data-backgroundImg
                2.2.1 For color, just add '#' in front of color. Need to be hex code.
            2.3. Make all divs (except the first), "style='display: none'"
        
        3. Bottom of #pageSliderMain, create 2 more divs for progress bar.
            3.1 #imageSliderProgressBarContainer, assign it class "progress"
            3.2     #imageSliderProgressBar inside the container, assign class="progress-bar"
                3.2.1 assign following attributes role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%"
        
        4. Finally, Call "InitializePageMainSlider();"
        5. If fullPageMode=true, targets ENTIRE top nav(#mainNavBar), if not, targets #pageSliderMain
    */
    function initializePageMainSlider(fullPageMode)
    {
        var divToUseForBackground = fullPageMode ? rj.find("#mainNavBar") : rj.find("#pageSliderMain");
        assignProperImageToSlider(divToUseForBackground, rj.Id("psm1"));

        var totalSliders = rj.find("#pageSliderMain .container").length;
        if (totalSliders > 1)
        {
            rj.Id('imageSliderProgressBarContainer').style.display = "block";
            main.Settings.currentImageProgressBar = rj.Id("imageSliderProgressBar");

            rj.AddEventListener('mouseover', '#pageSliderMain', function () { main.Settings.allowProgressionOnProgressBar = false; });
            rj.AddEventListener('mouseout', '#pageSliderMain', function () { main.Settings.allowProgressionOnProgressBar = true; });

            var iProgressBarCount = 0;
            var currentImageDisplayed = 1;
            setInterval(function ()
            {
                if (main.Settings.allowProgressionOnProgressBar)
                {
                    iProgressBarCount++;
                    if (iProgressBarCount > 99)
                    {
                        iProgressBarCount = -10; // Set it to lowerfor full "reset" effect.
                        rj.Id(rj.StringFormat("psm{0}", currentImageDisplayed)).style.display = "none";
                        currentImageDisplayed = (currentImageDisplayed >= totalSliders) ? 1 : currentImageDisplayed + 1;

                        var slide = rj.Id(rj.StringFormat("psm{0}", currentImageDisplayed));
                        slide.style.display = "block";

                        assignProperImageToSlider(divToUseForBackground, slide);
                    }
                    else
                    {
                        if (main.Settings.currentImageProgressBar !== null)
                        {
                            main.Settings.currentImageProgressBar.style.width = (iProgressBarCount < 1) ? "0%" : iProgressBarCount + '%';
                        }
                    }
                }
            }, 100);
        }
    }

    function assignProperImageToSlider(divObject, slide)
    {
        if (slide.hasAttribute("data-backgroundimg"))
        {
            var attResult = slide.getAttribute("data-backgroundimg");
            if (rj.StringContains(attResult, "#"))
            {
                // No image, just change background color.
                divObject.style.backgroundColor = attResult;
            }
            else
            {
                var linearGradient = slide.hasAttribute("data-lineargradient") ? slide.getAttribute("data-lineargradient") : "linear-gradient(rgba(0,0,0,0.7), rgba(0,0,0,0.4))";
                divObject.style.backgroundImage = rj.StringFormat("{0},url('{1}')", linearGradient, attResult);
                if (slide.hasAttribute('data-backgroundsize')) { divObject.style.backgroundSize = slide.getAttribute('data-backgroundsize'); }
                else { divObject.style.backgroundSize = 'unset'; }
            }
        }
    }
    //#endregion

    //#region Pega Mini Slider (Descriptions, Reviews, Etc)
    function initializePegaMiniSlider(targetId)
    {
        var nodes = rj.find(rj.StringFormat('#{0} div[data-pegaminislider]', targetId));
        var nextMiniSlideEvent = new CustomEvent(rj.StringFormat('{0}.mini.next.slide', targetId));
        var previousMiniSlideEvent = new CustomEvent(rj.StringFormat('{0}.mini.previous.slide', targetId));

        main.Settings.customEventsArray[rj.StringFormat('{0}.mini.next', targetId)] = nextMiniSlideEvent;
        main.Settings.customEventsArray[rj.StringFormat('{0}.mini.previous', targetId)] = previousMiniSlideEvent;

        rj.AddEventListener(rj.StringFormat('{0}.mini.next.slide', targetId), document, function () { handleMiniSlideChangeEvent(nodes, true); });
        rj.AddEventListener(rj.StringFormat('{0}.mini.previous.slide', targetId), document, function () { handleMiniSlideChangeEvent(nodes, false); });
    }

    function handleMiniSlideChangeEvent(nodes, isNext)
    {
        var totalLength = nodes.length;
        var switchInitiated = false;
        for (var i = 0; i < totalLength; i++)
        {
            // If block, found the current active. Hide it and show the next.
            if (nodes[i].style.display === '' || nodes[i].style.display === 'block')
            {
                // If next Index is higher than totalLength, reset back to 0;
                // if not Next, do the opposite.
                var nextSlideIndex = isNext ? (((i + 1) >= totalLength) ? 0 : i + 1)
                                            : (((i - 1) < 0) ? totalLength - 1 : i - 1);

                rj.AnimateFadeOut(nodes[i], 10);
                nodes[i].style.display = 'none';
                rj.AnimateFadeIn(nodes[nextSlideIndex], 10);
                switchInitiated = true;
                break;
            }
        }

        // In case they all get hidden from multi-event error.
        if (switchInitiated === false) { rj.AnimateFadeIn(nodes[0], 10); }
    }

    return main;
    //#endregion
})();