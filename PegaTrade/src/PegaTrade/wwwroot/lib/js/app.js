const rj = new RefJS();
var app = (function ()
{
    'use strict';
    var lib = {};

    lib.Version = "0.1.2";

    //#region Page Loaded (Helps avoid scrips in partial)

    lib.View_FullCoinsLoaded = function ()
    {
        pega.Settings.TableKeys['holdingPortfolioTable'] = new List('portfolioCurrentHoldingCoins',
            {
                valueNames: ['name', 'profit'],
                page: 20,
                pagination: true
            });

        pega.Settings.TableKeys['soldPortfolioTable'] = new List('portfolioCurrentSoldCoins',
            {
                valueNames: ['name', 'profit'],
                page: 30,
                pagination: true
            });

        pega.InitializeTabs('mainCoinPortfolioTabs');
    }

    lib.Crypto_FullDashboardLoaded = function ()
    {
        pega.InitializeTabs('mainDashboardConvTabs');
    }

    lib.View_GetOfficialCoinLoaded = function ()
    {
        pega.InitializeTabs('officialCoinConvTabs');
    }

    //#endregion

    //#region General (Small scripts all over the app)

    // -- Login

    lib.SubmitCreateUserForm = function ()
    {
        if (rj.ValidateForm('createAccountForm', true))
        {
            pega.ButtonLoading('createUserBtn');
            rj.AsyncSubmitForm('createAccountForm', { isJson: true }).then(function (result)
            {
                if (result.ResultType === 0) { window.location.href = rj.UrlAction("Dashboard"); }
                else
                {
                    rj.Id('createMessageError').innerHTML = result.Message;
                }
            }).catch(pega.OnFetchError).finally(function ()
            {
                pega.ButtonReset('createUserBtn');
                resetGRecaptcha();
            });
        }
    }

    lib.SubmitLoginForm = function ()
    {
        if (rj.ValidateForm('loginAccountForm', true))
        {
            pega.ButtonLoading('userLoginBtn');
            rj.AsyncSubmitForm('loginAccountForm', { isJson: true }).then(function (result)
            {
                if (result.ResultType === 0) { window.location.href = rj.UrlAction("Dashboard"); }
                else
                {
                    rj.Id('loginMessageError').innerHTML = result.Message;
                }
            }).catch(pega.OnFetchError).finally(function ()
            {
                pega.ButtonReset('userLoginBtn');
                resetGRecaptcha();
            });
        };
    }

    lib.SubmitPasswordResetForm = function ()
    {
        if (rj.ValidateForm('passwordResetForm', true))
        {
            pega.ButtonLoading('passwordResetBtn');
            rj.AsyncSubmitForm('passwordResetForm', { isJson: true }).then(function (result)
            {
                rj.Id('passwordResetErrorMessage').innerHTML = result.Message;
                rj.Id('passwordResetBtn').outerHTML = "";
                pega.DisplayResultsItem(result);
            }).catch(function (ex)
            {
                pega.OnFetchError(ex);
                pega.ButtonReset('passwordResetBtn');
            });
        };
    }

    lib.SubmitChooseNewPasswordForm = function ()
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncSubmitForm('chooseNewPasswordForm', { isJson: true }).then(function (result)
        {
            if (pega.DisplayResultsItem(result))
            {
                window.location.href = rj.UrlAction("Index", "Home");
            }
        }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.SubmitChangePasswordForm = function ()
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncSubmitForm('changePasswordForm', { isJson: true }).then(function (result)
        {
            if (pega.DisplayResultsItem(result))
            {
                window.location.href = rj.UrlAction("Index", "Home");
            }
        }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    //#endregion

    //#region Program Functionalities

    lib.PortfolioHoldingsTabChanged = function (tabType)
    {
        rj.Show('#holdingSummaryBlocks', false, false);
        rj.Show('#soldSummaryBlocks', false, false);

        if (tabType === 'current')
        {
            rj.Show('#holdingSummaryBlocks', true, false);
        }
        else if (tabType === 'sold')
        {
            rj.Show('#soldSummaryBlocks', true, false);
        }
    }

    lib.ReloadPortfolio = function ()
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncLoad('partialBody', rj.UrlAction('Portfolio', 'Crypto')).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.ReloadPortfolioHoldingsCoinSummary = function ()
    {
        pega.ShowSiteWideOverlay();

        var showCombined = rj.find('input[name="portfolioCoinsDisplayType"]')[0].checked;
        var currencyType = rj.Id('displayCurrencyFormatSL').value;

        if (rj.Id('currentPortfolioViewOtherUser').value === "True")
        {
            var username = rj.Id('currentViewUserUsernameInput').value;
            var portfolioName = rj.GetSelectListText('#globalCurrentlySelectedPortfolio');

            rj.AsyncLoad('coinsSummary', rj.UrlAction("LoadPortfolioViewMode", "Crypto"), {
                username: username, portfolioName: portfolioName, UseCombinedDisplay: showCombined, Currency: currencyType, coinsOnly: true
            }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
        }
        else
        {
            var portfolioId = app.GetCurrentPortfolioId();

            rj.AsyncLoad('coinsSummary', rj.UrlAction('GetAllCoinsFromPortfolio', 'Crypto'), {
                portfolioId: portfolioId, useCombinedDisplay: showCombined, currency: currencyType
            }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
        }
    }

    lib.IsValidSymbol = function (symbol)
    {
        symbol = symbol.toUpperCase();
        return rj.IsValidValue(symbol) && (symbol.startsWith("BTC-") || symbol.startsWith("ETH-") || symbol.startsWith("USD-") || symbol.startsWith("USDT-"))
    }

    lib.GetLatestPriceOfCoinBasedOnSymbol = function (symbol, outputId)
    {
        if (!app.IsValidSymbol(symbol))
        {
            alertify.error("Please enter a valid symbl. E.g. BTC-XRP");
            return;
        }

        rj.AsyncGet(rj.UrlAction("GetCurrentCoinPrice", "Crypto"), { symbol: symbol }).then(function (result)
        {
            if (result === 0) { alertify.error("Unable to get the latest price based on Symbol. Please enter manually."); return; }
            rj.Id(outputId).value = result;
        }).catch(pega.OnFetchError);
    }

    lib.GetWatchOnlyDetailsBasedOnSymbol = function (symbol)
    {
        if (!app.IsValidSymbol(symbol))
        {
            alertify.error("Please enter a valid symbl. E.g. BTC-XRP");
            return;
        }

        rj.AsyncGetJson(rj.UrlAction("GetGeneratedWatchOnlyDetails", "Crypto"), { symbol: symbol }).then(function (result)
        {
            if (!rj.IsNullOrUndefined(result.ResultType)) // If results type exists, probably an error.
            {
                pega.DisplayResultsItem(result);
                return;
            }

            rj.Id('Coin_Shares').value = result.quantity;
            rj.Id('coinPricePerUnitAddFormInput').value = result.pricePerUnit;
            rj.Id('Coin_TotalPricePaidUSD').value = 100;
        }).catch(pega.OnFetchError);
    }

    lib.GetCurrentPortfolioId = function () { return rj.Id('globalCurrentlySelectedPortfolio').value; }

    lib.ViewUserProfile = function (username)
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncLoad('partialBody', rj.UrlAction('GetViewUser', 'Account'), { username: username }).finally(pega.HideSiteWideOverlay);
    }

    //#endregion

    //#region CRUD, Submit Forms

    lib.PTOpenLoginForm = function (returnUrl)
    {
        pega.LoadModal("#mainModal", rj.UrlAction("ModalLogin", "Account"), { returnUrl: returnUrl },
            {
                Title: "Login or Create an account",
                HideFooter: true
            }).then(function ()
            {
                grecaptcha.render('recaptcha-login');
            });
    }

    lib.PTOpenCreateForm = function ()
    {
        pega.LoadModal("#mainModal", rj.UrlAction("Create", "Account"), null,
            {
                Title: "Create a new account",
                HideFooter: true
            }).then(function ()
            {
                grecaptcha.render('recaptcha-signup');
            });;
    }

    lib.PTOpenForgotPassowrdForm = function ()
    {
        pega.LoadModal("#mainModal", rj.UrlAction("PasswordResetForm", "Account"), null,
            {
                Title: "Forgot your password?",
                HideFooter: true
            });
    }

    lib.SubmitPortfolioManagementForm = function ()
    {
        if (rj.ValidateForm('portfolioManagementForm'))
        {
            rj.AsyncSubmitForm('portfolioManagementForm', { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    pega.Settings.secondModalObject.hide();
                    lib.RefreshCurrentPortfolioList(true);
                }
            }).catch(pega.OnFetchError);
        }
    }

    lib.ReOpenPortfolioListModam = function ()
    {
        rj.RemoveEventListener('hidden.bs.modal', '#secondModal', app.ReOpenPortfolioListModam);
        setTimeout(function ()
        {
            pega.LoadModal('#mainModal', rj.UrlAction("GetPortfolioList", "Crypto"), { isUpdated: true },
                {
                    Size: "mid",
                    Title: "Manage Portfolios",
                    HideFooter: true
                });
        }, 300);
    }

    lib.RefreshCurrentPortfolioList = function (updateUser)
    {
        if (rj.IsNullOrUndefined(updateUser)) { updateUser = false; }
        rj.AsyncGetJson(rj.UrlAction('GetAllPortfolioSelectList', 'Crypto'), { updateUser: updateUser }).then(function (result)
        {
            var list = rj.Id('globalCurrentlySelectedPortfolio');
            var currentValue = list.value;
            rj.PopulateSelectList(list, result, true);
            if (rj.SelectListContains(list, currentValue))
            {
                list.value = currentValue;
            }
        }).catch(pega.OnFetchError);
    }

    lib.DeletePortfolio = function (portfolioId)
    {
        alertify.confirm("Are you sure that you want to delete this portfolio?", function ()
        {
            pega.ShowSiteWideOverlay();
            rj.AsyncPost(rj.UrlAction('DeletePortfolio', 'Crypto'), { portfolioId: portfolioId }, { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    pega.Settings.mainModalObject.hide();
                    lib.ReloadPortfolio();
                }
            }).catch(pega.OnFetchError);
        });
    }

    lib.PTOpenAddNewCoinForm = function (isUpdated)
    {
        if (isUpdated == null || isUpdated == undefined) { isUpdated = false; }
        pega.LoadModal('mainModal', rj.UrlAction('AddNewCoins', 'Crypto'), { portfolioId: app.GetCurrentPortfolioId(), isUpdated: isUpdated },
            {
                HideFooter: true,
                Title: "Add/Import Coins"
            });
    }

    lib.PTOpenUpdateCoinForm = function (coinId, isCombined)
    {
        if (isCombined == 'True')
        {
            alertify.error("This coin is currently in Combined mode. Please select DisplayType: 'Seperate' to edit this coin.");
            return;
        }

        pega.LoadModal('mainModal', rj.UrlAction('UpdateCoin', 'Crypto'), { coinId: coinId },
            {
                HideFooter: true,
                Title: "Update Coins or Mask as sold"
            });
    }

    lib.SubmitCoinMaskAsSoldForm = function ()
    {
        if (rj.ValidateForm('coinMarkAsSoldForm'))
        {
            rj.AsyncSubmitForm('coinMarkAsSoldForm', { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    pega.Settings.mainModalObject.hide();
                    setTimeout(app.ReloadPortfolioHoldingsCoinSummary, 300);
                }
            }).catch(pega.OnFetchError);
        }
    }

    lib.SubmitCoinManagementForm = function ()
    {
        if (rj.ValidateForm('coinManagementForm'))
        {
            rj.AsyncSubmitForm('coinManagementForm', { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    pega.Settings.mainModalObject.hide();
                    setTimeout(app.ReloadPortfolioHoldingsCoinSummary, 300);
                }
            }).catch(pega.OnFetchError);
        }
    }

    lib.SubmitImportTradeForm = function ()
    {
        var fileSelectObj = rj.find('#import-trade-file-select');
        var csvFile = fileSelectObj.files[0];

        if (csvFile == null || (!csvFile.name.endsWith(".csv") && !csvFile.name.endsWith(".xlsx")) || csvFile.size > 100000)
        {
            alertify.error("Please select a proper .csv/.xlsx file that is less than 100kb.");
            return;
        }

        var formData = new FormData();
        formData.append('file', csvFile, csvFile.name);
        formData.append('exchange', rj.Id('import-exchange-csv-sl').value);
        formData.append('portfolioId', app.GetCurrentPortfolioId());

        if (rj.ValidateForm('importTradeHistoryForm', true))
        {
            pega.ButtonLoading('submitImportTradeFormBtn');
            rj.AsyncPost(rj.UrlAction("PostCSVTradeHistoryFile", "Crypto"), formData, { isJson: true }).then(function (result)
            {
                pega.ButtonReset('submitImportTradeFormBtn');
                if (pega.DisplayResultsItem(result))
                {
                    pega.Settings.mainModalObject.hide();
                    setTimeout(app.ReloadPortfolioHoldingsCoinSummary, 300);
                }
            }).catch(pega.OnFetchError);
        }
    }

    lib.ImportAPIActionSelectChange = function ()
    {
        var showExistingApiForm = rj.Id('importAPIexistingRB').checked;
        rj.Show('#useExistingImportSection', showExistingApiForm);
        rj.Show('#importFromApiFormContainer', !showExistingApiForm);
    }

    lib.SubmitAddImportAPIForm = function ()
    {
        if (rj.ValidateForm('importFromApiForm'))
        {
            pega.ShowSiteWideOverlay();
            rj.AsyncSubmitForm('importFromApiForm', { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    pega.Settings.mainModalObject.hide();
                    setTimeout(function () { app.PTOpenAddNewCoinForm(true) }, 300);
                }
            }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);;
        }
    }

    lib.SubmitImportEtherAddressForm = function ()
    {
        pega.ShowSiteWideOverlay();
        rj.Id('etherAddressFormPortfolioId').value = lib.GetCurrentPortfolioId();
        rj.AsyncSubmitForm('importFromEtherAddressForm', { isJson: true }).then(function (result)
        {
            if (pega.DisplayResultsItem(result))
            {
                pega.Settings.mainModalObject.hide();
                setTimeout(app.ReloadPortfolioHoldingsCoinSummary, 300);
            }
        }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.SubmitAPIsyncRequest = function ()
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncPost(rj.UrlAction('ImportSyncAPI', 'Crypto'), { apiId: rj.Id('availaleExistingImportApiList').value, portfolioId: app.GetCurrentPortfolioId() }, { isJson: true }).then(function (result)
        {
            if (pega.DisplayResultsItem(result))
            {
                pega.Settings.mainModalObject.hide();
                setTimeout(app.ReloadPortfolioHoldingsCoinSummary, 300);
            }
        }).catch(pega.OnFetchError);
    }

    lib.SubmitAPIDeleteRequest = function ()
    {
        var apiName = rj.GetSelectListText('#availaleExistingImportApiList');
        alertify.confirm(rj.StringFormat("Are you sure that you want to delete this Import API: {0}?", apiName), function ()
        {
            pega.ShowSiteWideOverlay();
            rj.AsyncPost(rj.UrlAction('DeleteImportAPI', 'Crypto'), { apiId: rj.GetSelectListValue('#availaleExistingImportApiList') }, { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    rj.DeleteSelctListItem('#availaleExistingImportApiList', rj.GetSelectListValue('#availaleExistingImportApiList'));
                }
            }).catch(pega.OnFetchError);
        });
    }

    //#endregion

    //#region Community (+ CRUD)

    lib.ViewAllThreadsInMainDashboard = function ()
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncLoad('mainDashboardAllConvTab', rj.UrlAction('GetAllConversationThreads', 'Community')).finally(pega.HideSiteWideOverlay);
    }

    lib.OpenThreadWithComments = function (threadId)
    {
        pega.LoadModal('mainModal', rj.UrlAction("GetThreadsWithComments", "Community"), { threadId: threadId },
            {
                Title: "Conversations",
                HideFooter: true,
                allowCloseOnBackground: true
            });
    }

    lib.SubmitCreateThreadForm = function (formId)
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncSubmitForm(formId, { isJson: true }).then(function (result)
        {
            if (pega.DisplayResultsItem(result))
            {
                var currForm = rj.Id(formId);
                var categoryCode = currForm.querySelector("input[name='CategoryCode']").value;
                var officialCoinId = currForm.querySelector("input[name='OfficialCoinId']").value;
                var threadName = currForm.querySelector("input[name='ThreadName']").value;

                rj.AsyncLoad('conversationsHolder', rj.UrlAction("GetConversationThreads", "Community"), { threadName: threadName, category: categoryCode, officialCoinId: officialCoinId });
            }
        }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.SubmitCreateCommentForm = function (formId)
    {
        pega.ShowSiteWideOverlay();
        rj.AsyncSubmitForm(formId, { isJson: true }).then(function (result)
        {
            if (pega.DisplayResultsItem(result))
            {
                var currForm = rj.Id(formId);
                var threadId = currForm.querySelector("input[name='ThreadId']").value;
                var target = rj.find("#mainModal .modal-body")[0];

                rj.AsyncLoad(target, rj.UrlAction("GetThreadsWithComments", "Community"), { threadId: threadId });
            }
        }).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.DeleteBBThread = function (threadId)
    {
        alertify.confirm("Are you sure that you want to delete this thread?", function ()
        {
            pega.ShowSiteWideOverlay();
            rj.AsyncPost(rj.UrlAction('DeleteBBThread', 'Community'), { threadId: threadId }, { isJson: true }).then(pega.DisplayResultsItem).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
        });
    }

    lib.DeleteBBComment = function (commentId)
    {
        alertify.confirm("Are you sure that you want to delete this comment?", function ()
        {
            pega.ShowSiteWideOverlay();
            rj.AsyncPost(rj.UrlAction('DeleteBBComment', 'Community'), { commentId: commentId }, { isJson: true }).then(pega.DisplayResultsItem).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
        });
    }

    lib.SubmitThreadVote = function (threadId, isUpvote)
    {
        rj.AsyncPost(rj.UrlAction("VoteBBThread", "Community"), { threadId: threadId, isUpvote: isUpvote }, { isJson: true }).then(pega.DisplayResultsItem).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.SubmitCommentVote = function (commentId, isUpvote)
    {
        rj.AsyncPost(rj.UrlAction("VoteBBComment", "Community"), { commentId: commentId, isUpvote: isUpvote }, { isJson: true }).then(pega.DisplayResultsItem).catch(pega.OnFetchError).finally(pega.HideSiteWideOverlay);
    }

    lib.ShowHideBBMessageTextOptions = function (formId, show)
    {
        var target = rj.Id(formId);
        if (!show)
        {
            var messageText = rj.find(rj.StringFormat('#{0} textarea[data-input]', formId))[0];
            if (messageText.value.length > 0) { return; }
        }
        var options = rj.find(rj.StringFormat('#{0} div[data-options]', formId))[0];
        rj.Show(options, show);
    }

    // data-action(vote, open, delete, share), data-type(comment, thread), data-id(commentId, ThreadId), data-upvote
    lib.ConvOptionsClicked = function (obj, e)
    {
        e.preventDefault();
        e.stopPropagation();

        var action = obj.getAttribute("data-action");
        var actionType = obj.getAttribute("data-type");
        var actionId = obj.getAttribute("data-id");

        switch (action)
        {
            case "vote":
                var upvote = obj.getAttribute("data-upvote");
                if (actionType === 'comment') { lib.SubmitCommentVote(actionId, upvote); }
                else { lib.SubmitThreadVote(actionId, upvote); }
                break;

            case "open":
                app.OpenThreadWithComments(actionId);
                break;
        }
    };

    lib.ToggleConvOption = function (obj)
    {
        rj.FindChildren(rj.FindParent(obj, "div"), ".dark-app-dropdown .dropdown-content")[0].classList.toggle("show");
    }

    //#endregion

    //#region Utilities

    lib.PopulateTopBarHiddenMenu = function ()
    {
        executePopulateTopBarHiddenMenu('firstMenu', 'firstMenuTopHiddenUL');
        executePopulateTopBarHiddenMenu('secondMenu', 'secondMenuTopHiddenUL');
        executePopulateTopBarHiddenMenu('thirdMenu', 'thirdMenuTopHiddenUL');
        executePopulateTopBarHiddenMenu('fourthMenu', 'fourthMenuTopHiddenUL');
    }
    function executePopulateTopBarHiddenMenu(sourceId, targetId)
    {
        var target = rj.Id(targetId);
        rj.ForEach(rj.find(rj.StringFormat('#{0} a', sourceId)), function (item)
        {
            var innerHtml = item.innerHTML.trim();
            var lastIndex = innerHtml.lastIndexOf(">");
            var anchorText = innerHtml.substring(lastIndex + 1);
            var anchorUrl = item.getAttribute("data-url");

            rj.AppendHTML(target, rj.StringFormat('<li><a href="javascript:void(0)" data-loadpartial data-targetid="partialBody" data-url="{0}">{1}</a></li>', anchorUrl, anchorText));
        });
    }

    lib.SubmitContactUsForm = function ()
    {
        if (rj.ValidateForm('ContactUsForm', true))
        {
            pega.ButtonLoading('submitContactUsBtn');
            rj.AsyncSubmitForm('ContactUsForm', { isJson: true }).then(function (result)
            {
                if (pega.DisplayResultsItem(result))
                {
                    rj.Id('ContactUsForm').innerHTML = result.Message;
                    rj.Id('ContactUsForm').classList.add("text-success");
                    rj.Id('ContactUsForm').classList.add("alert-success");
                    rj.Id('ContactUsForm').style.padding = "15px"
                    return;
                }
            }).catch(function (ex)
            {
                alertify.error("Sending has failed. Please email us manually at support@PegaTrade.com.");
            }).finally(function ()
            {
                pega.ButtonReset('submitContactUsBtn');
                resetGRecaptcha();
            });
        }
    }

    lib.WindowsWidthChangeEvent = function (matchMediaQuery)
    {
        var isNowXSmode = !(matchMediaQuery.matches); // not match means it's sm-size
        app.HandleWidthChangeSideBar(isNowXSmode);
    }

    lib.HandleWidthChangeSideBar = function (isNowXSmode)
    {
        var menuItems = ['firstMenu', 'secondMenu', 'thirdMenu', 'fourthMenu'];
        rj.ForEach(menuItems, function (itemDivId)
        {
            var item = rj.Id(itemDivId);
            if (isNowXSmode)
            {
                item.classList.remove('collapse');
                item.classList.remove('side-menu-ul');
                item.classList.add('side-menu-ul-xs');
                item.classList.add('dropdown-menu');
            }
            else
            {
                item.classList.add('collapse');
                item.classList.add('side-menu-ul');
                item.classList.remove('side-menu-ul-xs');
                item.classList.remove('dropdown-menu');
            }
        });
    }

    lib.HideAdminSideBar = function (isHide)
    {
        if (isHide)
        {
            rj.Id('adminNavStaticSideBar').classList.add("hidden");
            rj.Id('page-wrapper').style.margin = "0 0 120px 0";
            rj.Show('#topNavAdminMenuHideBar', false);
            rj.Show('#topNavAdminMenuBar', true);
        }
        else
        {
            rj.Id('adminNavStaticSideBar').classList.remove("hidden");
            rj.Id('page-wrapper').style.removeProperty('margin');
            rj.Show('#topNavAdminMenuHideBar', true);
            rj.Show('#topNavAdminMenuBar', false);
        }
    }

    function resetGRecaptcha()
    {
        if (grecaptcha != undefined && grecaptcha != null)
        {
            grecaptcha.reset();
        }
    }

    lib.PegaTradeBetaWarning = function()
    {
        alertify.alert("PegaTrade is still in early release/beta version. It may work great for one user, but another user may run into several errors when importing trades (due to so many " +
            "possible scenario/ trade - pairs). While we are actively working on PegaTrade to make it as stable as possible, we encourage that you Contact us and report any errors that you may have ran into.");
    }

    //#endregion Utilities

    return lib;
})();

function ListJSCustomNumericSort(a, b)
{
    var before = parseFloat(a._values.profit.replace(/[^0-9.-]/g, ""));
    var after = parseFloat(b._values.profit.replace(/[^0-9.-]/g, ""));

    if (before > after) { return 1; }
    if (before < after) { return -1; }
    else { return 0; }
}