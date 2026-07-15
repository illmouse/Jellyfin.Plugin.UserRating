(function() {
    'use strict';

    console.log('[UserRatings] Loading plugin...');

    // CSS for inline ratings UI
    // Uses ElegantFin CSS custom properties with fallbacks for default Jellyfin
    const style = document.createElement('style');
    style.textContent = `
        #user-ratings-ui {
            grid-column: 1 / -1;
            min-height: 1px;
        }
        .user-ratings-container {
            background: var(--lighterGradientPointAlpha, rgba(0, 0, 0, 0.15));
            backdrop-filter: var(--blurDefault, blur(10px));
            border-radius: var(--largeRadius, 10px);
            padding: 1.8em 2em;
            margin-top: 1.5em;
            margin-bottom: 1.5em;
            border: var(--defaultBorder, 1px solid rgba(255, 255, 255, 0.08));
            box-sizing: border-box;
            color: var(--textColor, #ffffff);
        }
        .user-ratings-container * {
            box-sizing: border-box;
        }
        .user-ratings-header {
            font-size: 1.3em;
            font-weight: 500;
            margin-bottom: 1.2em;
            color: var(--textColor, #ffffff);
            display: flex;
            align-items: center;
            gap: 1em;
            flex-wrap: wrap;
        }
        .user-ratings-average {
            color: #ffd700;
            font-size: 1.1em;
        }
        .user-ratings-my-rating {
            margin-bottom: 1.5em;
            padding-bottom: 1.5em;
            border-bottom: var(--defaultBorder, 1px solid rgba(255, 255, 255, 0.08));
        }
        .user-ratings-section-title {
            font-size: 1.15em;
            margin-bottom: 0.3em;
            color: var(--textColor, #ffffff);
            font-weight: 600;
        }
        .user-ratings-section-subtitle {
            font-size: 0.9em;
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
            margin-bottom: 0.8em;
        }
        .rating-form-row {
            display: block;
            margin-top: 0.5em;
        }
        .rating-form-section {
            margin-bottom: 1.6em;
        }
        .star-rating-container {
            display: flex;
            align-items: center;
            gap: 0.8em;
            margin-bottom: 0.5em;
        }
        .star-rating {
            display: inline-flex;
            gap: 0.25em;
            cursor: pointer;
            font-size: 1.9em;
        }
        .star-rating .star {
            color: rgba(255, 255, 255, 0.15);
            transition: color 0.2s, transform 0.15s;
            cursor: pointer;
            filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.3));
        }
        .star-rating .star.filled {
            color: #ffd700;
        }
        .star-rating .star.half {
            background: linear-gradient(90deg, #ffd700 50%, rgba(255, 255, 255, 0.15) 50%);
            -webkit-background-clip: text;
            background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .star-rating .star:hover {
            color: #ffed4e;
            transform: scale(1.15);
        }
        .star-rating .star.half:hover {
            background: linear-gradient(90deg, #ffed4e 50%, rgba(255, 255, 255, 0.15) 50%);
            -webkit-background-clip: text;
            background-clip: text;
            -webkit-text-fill-color: transparent;
            transform: scale(1.15);
        }
        .rating-prompt {
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
            font-size: 0.9em;
        }
        .rating-note-input {
            width: 100%;
            padding: 0.5em;
            background: var(--selectorBackgroundColor, rgba(0, 0, 0, 0.2));
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.2));
            border-radius: var(--smallRadius, 8px);
            color: var(--textColor, #ffffff);
            font-size: 0.95em;
            font-family: inherit;
            transition: border-color 0.2s, background 0.2s;
            resize: vertical;
            min-height: 100px;
            line-height: 1.6;
        }
        .rating-note-input:focus {
            outline: none;
            border-color: var(--highlightOutlineColor, #00a4dc) !important;
            background: var(--selectorBackgroundColor, rgba(0, 0, 0, 0.3));
            box-shadow: 0 0 0 1px var(--highlightOutlineColor, #00a4dc);
        }
        .rating-note-input::placeholder {
            color: var(--dimTextColor, rgba(255, 255, 255, 0.35));
        }
        .rating-char-count {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255, 255, 255, 0.4));
            margin-top: 0.5em;
        }
        .rating-char-count.error {
            color: #ff6b6b;
        }
        .rating-actions {
            margin-top: 1.2em;
            display: flex;
            gap: 0.75em;
            flex-wrap: wrap;
        }
        .rating-actions button {
            position: relative;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            box-sizing: border-box;
            outline: 0;
            margin: 0;
            cursor: pointer;
            user-select: none;
            vertical-align: middle;
            text-decoration: none;
            font-family: inherit;
            font-weight: 500;
            font-size: 0.9375rem;
            line-height: 1.75;
            letter-spacing: 0.02857em;
            text-transform: uppercase;
            min-width: 64px;
            padding: 8px 22px;
            border-radius: var(--smallRadius, 4px);
            transition: background-color 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms, box-shadow 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms, border-color 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms, filter 125ms;
        }
        .rating-actions .save-btn {
            background-color: var(--btnSubmitColor, #e53935);
            color: #fff;
            border: solid var(--btnSubmitBorderColor, transparent) var(--borderWidth, 0.06em);
            box-shadow: 0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12);
            flex: 1;
            min-width: 200px;
        }
        .rating-actions .save-btn:hover:not(:disabled) {
            filter: brightness(1.2);
        }
        .rating-actions .save-btn:active:not(:disabled) {
            filter: brightness(1.2);
            transform: scale(0.98);
        }
        .rating-actions .save-btn:disabled {
            background-color: rgba(255, 255, 255, 0.12);
            color: rgba(255, 255, 255, 0.3);
            cursor: default;
            pointer-events: none;
            box-shadow: none;
            filter: none;
        }
        .rating-actions .delete-btn {
            background-color: var(--btnDeleteColor, transparent);
            color: var(--textColor, rgba(255, 255, 255, 0.7));
            border: solid var(--btnDeleteBorderColor, rgba(255, 255, 255, 0.23)) var(--borderWidth, 0.06em);
            padding: 7px 21px;
        }
        .rating-actions .delete-btn:hover {
            filter: brightness(1.2);
            background-color: var(--btnDeleteColor, rgba(255, 255, 255, 0.08));
        }
        .rating-actions .delete-btn:active {
            filter: brightness(1.2);
            transform: scale(0.98);
        }
        .user-ratings-all {
            margin-top: 1.5em;
        }
        .rating-item {
            margin: 0.75em 0;
            padding: 1em;
            background: var(--darkerGradientPointAlpha, rgba(0, 0, 0, 0.12));
            border-radius: var(--smallRadius, 8px);
            color: var(--textColor, #ffffff);
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.05));
        }
        .rating-item-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 0.5em;
            flex-wrap: wrap;
            gap: 0.5em;
        }
        .rating-item-user {
            font-weight: 500;
            color: var(--textColor, #ffffff);
        }
        .rating-item-stars {
            color: #ffd700;
            margin-left: 0.5em;
        }
        .rating-item-date {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
        }
        .rating-item-note {
            margin-top: 0.5em;
            opacity: 0.9;
            font-size: 0.95em;
            color: var(--dimTextColor, #e0e0e0);
            line-height: 1.4;
        }
        .user-ratings-my-rating.collapsed .rating-form-section,
        .user-ratings-my-rating.collapsed .rating-actions {
            display: none;
        }
        .my-rating-summary {
            display: none;
            align-items: center;
            gap: 0.6em;
            cursor: pointer;
            padding: 0.6em 0.9em;
            border-radius: var(--smallerRadius, 6px);
            transition: background 0.2s;
            margin-left: auto;
            flex-wrap: wrap;
        }
        .my-rating-summary:hover {
            background: rgba(255, 255, 255, 0.06);
        }
        .user-ratings-container.has-rating .my-rating-summary {
            display: flex;
        }
        .summary-stars {
            color: #ffd700;
            font-size: 1.1em;
            letter-spacing: 1px;
        }
        .summary-label {
            color: var(--dimTextColor, rgba(255, 255, 255, 0.6));
            font-size: 0.95em;
        }
        .edit-rating-btn {
            margin-left: auto;
            background: transparent;
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.23));
            color: var(--dimTextColor, rgba(255, 255, 255, 0.7));
            padding: 4px 14px;
            border-radius: var(--smallerRadius, 4px);
            cursor: pointer;
            font-size: 0.85em;
            font-family: inherit;
            transition: background 0.2s, border-color 0.2s;
        }
        .edit-rating-btn:hover {
            background: rgba(255, 255, 255, 0.08);
            border-color: var(--lighterBorderColor, rgba(255, 255, 255, 0.3));
        }
        .collapse-rating-btn {
            display: none;
            background: none;
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.23));
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
            padding: 2px 10px;
            border-radius: var(--smallerRadius, 4px);
            cursor: pointer;
            font-size: 0.8em;
            font-family: inherit;
            margin-left: 0.5em;
            transition: background 0.2s, color 0.2s;
        }
        .collapse-rating-btn:hover {
            background: rgba(255, 255, 255, 0.06);
            color: var(--textColor, rgba(255, 255, 255, 0.8));
        }
        .user-ratings-container.has-rating .collapse-rating-btn {
            display: inline-block;
        }
        @media (max-width: 480px) {
            .user-ratings-container {
                padding: 1em 0.8em;
                margin-top: 1em;
                margin-bottom: 1em;
                border-radius: var(--smallRadius, 8px);
            }
            .user-ratings-header {
                font-size: 1.1em;
                gap: 0.5em;
            }
            .my-rating-summary {
                margin-left: 0;
                flex-direction: column;
                align-items: flex-start;
                gap: 0.3em;
                width: 100%;
                padding: 0.5em 0.7em;
            }
            .edit-rating-btn {
                margin-left: 0;
                width: 100%;
                text-align: center;
                margin-top: 0.2em;
            }
            .star-rating {
                font-size: 1.5em;
            }
            .rating-actions {
                flex-direction: column;
                gap: 0.5em;
            }
            .rating-actions .save-btn,
            .rating-actions .delete-btn {
                min-width: unset;
                flex: unset;
                width: 100%;
                text-align: center;
                padding: 8px 22px;
            }
            .rating-note-input {
                min-height: 80px;
                padding: 0.8em;
            }
            .rating-item {
                padding: 0.75em;
            }
        }

        /* ===== RATE BADGE (unrated cards) ===== */
        .rate-badge {
            background: linear-gradient(135deg, #43a047, #2e7d32);
            border: 1px solid rgba(255,255,255,0.15);
            border-radius: 6px;
            padding: 0.35em 0.8em;
            display: inline-flex;
            align-items: center;
            gap: 0.3em;
            cursor: pointer;
            font-weight: 600;
            font-size: 0.85em;
            color: #fff;
            box-shadow: 0 2px 6px rgba(0,0,0,0.3);
            transition: transform 0.12s ease, box-shadow 0.12s ease, background 0.12s ease;
            pointer-events: auto;
            user-select: none;
        }
        .rate-badge:hover {
            background: linear-gradient(135deg, #4caf50, #388e3c);
            transform: translateY(-1px) scale(1.05);
            box-shadow: 0 4px 12px rgba(76,175,80,0.4);
            border-color: rgba(255,255,255,0.3);
        }
        .rate-badge:active {
            transform: translateY(0) scale(0.98);
            box-shadow: 0 1px 3px rgba(0,0,0,0.3);
        }

        /* ===== COMPACT RATING BADGE (rated cards) ===== */
        .compact-rating {
            position: absolute;
            top: 0.4em; left: 0.4em;
            z-index: 3;
            display: inline-flex;
            align-items: center;
            gap: 0.25em;
            background: rgba(0,0,0,0.7);
            padding: 0.2em 0.5em;
            border-radius: 3px;
            font-size: 0.8em;
            line-height: 1.5;
            cursor: pointer;
            pointer-events: auto;
            transition: background 0.15s;
        }
        .compact-rating:hover {
            background: rgba(0,0,0,0.9);
        }
        .compact-rating .cr-heart {
            color: #e53935;
            font-size: 1.1em;
        }
        .compact-rating .cr-value {
            font-weight: 600;
            color: #fff;
        }
        .compact-rating .cr-edit {
            opacity: 0;
            transition: opacity 0.2s;
            color: rgba(255,255,255,0.4);
            font-size: 0.85em;
            margin-left: 0.15em;
        }
        .card-hoverable:hover .compact-rating .cr-edit {
            opacity: 1;
        }
        .compact-rating[data-empty="true"] {
            display: none;
        }

        /* ===== RATE SUCCESS ANIMATION ===== */
        @keyframes rate-success-pulse {
            0% { box-shadow: 0 0 0 0 rgba(255,215,0,0.6); }
            50% { box-shadow: 0 0 0 6px rgba(255,215,0,0.2); }
            100% { box-shadow: 0 0 0 0 rgba(255,215,0,0); }
        }
        .card-rating-success {
            animation: rate-success-pulse 0.6s ease-out;
        }

        /* ===== AVERAGE RATING BADGE (shared across all card surfaces) ===== */
        .ur-avg-badge {
            position: absolute;
            top: 0.4em;
            left: 0.4em;
            z-index: 3;
            background: rgba(0,0,0,0.85);
            padding: 0.4em 0.7em;
            border-radius: 4px;
            display: inline-flex;
            align-items: center;
            gap: 0.3em;
            pointer-events: none;
            user-select: none;
        }
        /* When avg badge present, stack personal below it */
        .card[data-ur-has-avg="1"] .compact-rating {
            top: 2.8em;
        }
        .ur-avg-badge .ur-avg-star {
            color: #ffd700;
            font-size: 1.1em;
        }
        .ur-avg-badge .ur-avg-value {
            font-weight: 600;
        }
        .ur-avg-badge .ur-avg-count {
            opacity: 0.7;
            font-size: 0.85em;
        }

        /* ===== MOBILE: shrink badges (already top-left stacked on all screens) ===== */
        @media (max-width: 480px) {
            .compact-rating {
                font-size: 0.65em;
                padding: 0.15em 0.35em;
                gap: 0.15em;
                line-height: 1.2;
            }
            .compact-rating .cr-heart {
                font-size: 1em;
            }
            .compact-rating .cr-value {
                font-size: 1em;
            }
            .ur-avg-badge {
                font-size: 0.6em;
                padding: 0.15em 0.35em;
                gap: 0.2em;
            }
            .ur-avg-badge .ur-avg-star {
                font-size: 1em;
            }
            .ur-avg-badge .ur-avg-count {
                display: none;
            }
            .card[data-ur-has-avg="1"] .compact-rating {
                top: 3.0em;
            }
        }

        /* ===== DETAIL PAGE RATING BADGE (next to IMDb rating) ===== */
        .ur-detail-badge {
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: 0.25em;
            color: var(--textColor, #ddd);
            transition: color 0.15s, background 0.15s;
            border-radius: 4px;
            padding: 0.1em 0.3em;
            user-select: none;
        }
        .ur-detail-badge:hover {
            color: #fff;
            background: rgba(255,255,255,0.08);
        }
        .ur-detail-badge .ur-db-star {
            color: #ffd700;
            font-size: 1em;
        }
        .ur-detail-badge .ur-db-mine {
            color: inherit;
            font-weight: 600;
        }
        .ur-detail-badge .ur-db-heart {
            color: #e53935;
            font-size: 1em;
        }
        .ur-detail-badge .ur-db-sep {
            opacity: 0.4;
            margin: 0 0.15em;
        }
        .ur-detail-badge .ur-db-rate {
            opacity: 0.7;
            font-size: 0.9em;
        }
        .ur-detail-badge:hover .ur-db-rate {
            opacity: 1;
        }

        /* ===== RATINGS SECTION (bottom of detail page, Cast/Similar style) ===== */
        #urRatingsCollapsible .sectionTitle {
            margin-bottom: 0.4em;
        }
        #urRatingsCollapsible .emby-scrollbuttons {
            display: inline-flex;
            vertical-align: middle;
            padding-top: .4em;
        }
        #urRatingsCollapsible .ur-scroller {
            overflow-x: auto;
            overflow-y: hidden;
            scroll-behavior: smooth;
            -webkit-overflow-scrolling: touch;
            padding-bottom: 0.3em;
        }
        #urRatingsCollapsible .ur-items {
            white-space: nowrap;
        }
        #urRatingsCollapsible .ur-scroll-btn {
            -webkit-align-items: center;
            align-items: center;
            background: transparent;
            border: 0;
            border-radius: 50%;
            box-sizing: border-box;
            color: inherit;
            cursor: pointer;
            display: inline-flex;
            font-family: inherit;
            font-size: inherit;
            margin: 0 .29em;
            padding: .556em;
            outline: none !important;
            overflow: hidden;
        }
        #urRatingsCollapsible .ur-scroll-btn .material-icons {
            font-size: 1.6695652174em;
            vertical-align: middle;
        }
        #urRatingsCollapsible .ur-scroll-btn:disabled,
        #urRatingsCollapsible .ur-scroll-btn.is-disabled {
            cursor: default;
            opacity: .3;
        }
        #urRatingsCollapsible .ur-rating-card {
            width: 220px;
            background: var(--cardBackground, rgba(20,20,20,0.5));
            border: 1px solid rgba(255,255,255,0.08);
            border-radius: 8px;
            padding: 0.8em;
            cursor: pointer;
            transition: transform 0.15s, box-shadow 0.15s, border-color 0.15s;
            display: inline-flex;
            flex-direction: column;
            gap: 0.4em;
            vertical-align: top;
            margin-right: 0.4em;
            white-space: normal;
        }
        #urRatingsCollapsible .ur-rating-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 18px rgba(0,0,0,0.4);
            border-color: rgba(255,255,255,0.18);
        }
        #urRatingsCollapsible .ur-rc-header {
            display: flex;
            align-items: center;
            gap: 0.5em;
        }
        #urRatingsCollapsible .ur-rc-avatar {
            width: 32px;
            height: 32px;
            border-radius: 50%;
            background: linear-gradient(135deg, #00a4dc, #aa5ccf);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: 600;
            font-size: 0.9em;
            flex-shrink: 0;
            overflow: hidden;
        }
        #urRatingsCollapsible .ur-rc-avatar img {
            width: 100%;
            height: 100%;
            border-radius: 50%;
            object-fit: cover;
        }
        #urRatingsCollapsible .ur-rc-name {
            font-weight: 600;
            font-size: 0.95em;
            color: var(--textColor, #fff);
            flex: 1 1 auto;
            min-width: 0;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        #urRatingsCollapsible .ur-rc-stars {
            color: #ffd700;
            font-size: 0.95em;
            letter-spacing: 1px;
        }
        #urRatingsCollapsible .ur-rc-date {
            font-size: 0.75em;
            opacity: 0.6;
            margin-left: auto;
        }
        #urRatingsCollapsible .ur-rc-comment {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255,255,255,0.6));
            line-height: 1.4;
            display: -webkit-box;
            -webkit-line-clamp: 3;
            -webkit-box-orient: vertical;
            overflow: hidden;
        }

        /* ===== RATING DETAILS POPUP (read-only) ===== */
        .ur-rating-details-overlay {
            display: none;
            position: fixed;
            inset: 0;
            background: rgba(0,0,0,0.6);
            z-index: 10000;
            align-items: center;
            justify-content: center;
        }
        .ur-rating-details-overlay.open {
            display: flex;
        }
        .ur-rating-details-popup {
            background: #1e1e1e;
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 14px;
            padding: 1.6em 1.8em;
            width: 420px;
            max-width: 92vw;
            box-shadow: 0 12px 40px rgba(0,0,0,0.5);
            animation: popup-slide-in 0.2s ease-out;
        }
        .ur-rating-details-popup .ur-rdp-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.8em;
        }
        .ur-rating-details-popup .ur-rdp-user {
            display: flex;
            align-items: center;
            gap: 0.6em;
        }
        .ur-rating-details-popup .ur-rdp-avatar {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            background: linear-gradient(135deg, #00a4dc, #aa5ccf);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: 600;
            font-size: 1em;
            overflow: hidden;
        }
        .ur-rating-details-popup .ur-rdp-avatar img {
            width: 100%;
            height: 100%;
            border-radius: 50%;
            object-fit: cover;
        }
        .ur-rating-details-popup .ur-rdp-name {
            font-weight: 600;
            color: var(--textColor, #fff);
        }
        .ur-rating-details-popup .ur-rdp-date {
            font-size: 0.8em;
            opacity: 0.6;
        }
        .ur-rating-details-popup .ur-rdp-close {
            background: none;
            border: none;
            color: rgba(255,255,255,0.4);
            font-size: 1.4em;
            cursor: pointer;
            padding: 0 0.2em;
            line-height: 1;
        }
        .ur-rating-details-popup .ur-rdp-close:hover { color: #fff; }
        .ur-rating-details-popup .ur-rdp-stars {
            color: #ffd700;
            font-size: 1.3em;
            letter-spacing: 2px;
            margin-bottom: 0.8em;
        }
        .ur-rating-details-popup .ur-rdp-note {
            color: var(--textColor, #ddd);
            line-height: 1.5;
            white-space: pre-wrap;
        }

        /* ===== RATE POPUP MODAL ===== */
        .rate-popup-overlay {
            display: none;
            position: fixed;
            inset: 0;
            background: rgba(0,0,0,0.6);
            z-index: 10000;
            align-items: center;
            justify-content: center;
        }
        .rate-popup-overlay.open {
            display: flex;
        }
        .rate-popup {
            background: #1e1e1e;
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 14px;
            padding: 1.6em 1.8em 1.4em;
            width: 380px;
            max-width: 92vw;
            box-shadow: 0 12px 40px rgba(0,0,0,0.5);
            animation: popup-slide-in 0.2s ease-out;
        }
        @keyframes popup-slide-in {
            from { opacity: 0; transform: scale(0.95) translateY(12px); }
            to { opacity: 1; transform: scale(1) translateY(0); }
        }
        .rate-popup-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.4em;
        }
        .rate-popup-title {
            font-size: 1.1em;
            font-weight: 600;
            color: var(--textColor, #fff);
        }
        .rate-popup-close {
            background: none;
            border: none;
            color: rgba(255,255,255,0.4);
            font-size: 1.4em;
            cursor: pointer;
            padding: 0 0.2em;
            line-height: 1;
        }
        .rate-popup-close:hover {
            color: #fff;
        }
        .rate-popup-subtitle {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255,255,255,0.5));
            margin-bottom: 1em;
        }
        .rate-popup-stars {
            display: flex;
            gap: 0.3em;
            font-size: 2em;
            justify-content: center;
            margin-bottom: 1em;
        }
        .rate-popup-stars .rp-star {
            background: linear-gradient(90deg, #ffd700 var(--star-fill, 0%), rgba(255,255,255,0.15) var(--star-fill, 0%));
            -webkit-background-clip: text;
            background-clip: text;
            color: transparent;
            cursor: pointer;
            transition: transform 0.12s;
        }
        .rate-popup-stars .rp-star.hover-highlight {
            filter: brightness(1.25);
        }
        .rate-popup-stars .rp-star:hover {
            transform: scale(1.15);
        }
        .rate-popup textarea {
            width: 100%;
            padding: 0.6em;
            background: rgba(0,0,0,0.3);
            border: 1px solid rgba(255,255,255,0.12);
            border-radius: 8px;
            color: var(--textColor, #ddd);
            font-size: 0.9em;
            font-family: inherit;
            resize: vertical;
            min-height: 70px;
            margin-bottom: 1em;
            line-height: 1.5;
        }
        .rate-popup textarea:focus {
            outline: none;
            border-color: var(--highlightOutlineColor, #00a4dc);
        }
        .rate-popup-actions {
            display: flex;
            gap: 0.6em;
            justify-content: flex-end;
        }
        .rate-popup-actions button {
            padding: 0.5em 1.4em;
            border-radius: 6px;
            border: none;
            font-size: 0.9em;
            font-weight: 500;
            cursor: pointer;
            font-family: inherit;
            transition: filter 0.15s;
        }
        .rate-popup-actions button:hover {
            filter: brightness(1.15);
        }
        .rate-popup-btn-cancel {
            background: rgba(255,255,255,0.08);
            color: var(--textColor, #ddd);
            border: 1px solid rgba(255,255,255,0.15) !important;
        }
        .rate-popup-btn-submit {
            background: var(--btnSubmitColor, #e53935);
            color: #fff;
            min-width: 120px;
        }
        .rate-popup-btn-submit:disabled {
            opacity: 0.4;
            cursor: default;
            filter: none;
        }
        .rate-popup-btn-delete {
            background: transparent;
            color: #e53935;
            border: 1px solid rgba(229,57,53,0.3) !important;
            margin-right: auto;
        }
        .rate-popup-btn-delete:hover {
            background: rgba(229,57,53,0.1);
        }
        @media (max-width: 480px) {
            .rate-popup {
                padding: 1.2em;
            }
            .rate-popup-stars {
                font-size: 1.6em;
            }
        }
    `;
    document.head.appendChild(style);

    let currentItemId = null;
    let currentRating = 0;
    let isInjecting = false;
    let userRatingsMap = null;
    let popupModal = null;
    let popupActiveItemId = null;

    const AVG_CACHE_MAX = 500;
    let avgCache = new Map();
    let avgCacheOrder = [];
    let _batchInFlight = null;
    let _batchQueuedIds = new Set();
    let _decorateTimer = null;
    let _userRatingsPrimed = false;
    let _fetchPromise = null;

    function ensureUserRatings() {
        if (!_fetchPromise) {
            _fetchPromise = fetchUserRatings().catch(function() {}).then(function() {
                _userRatingsPrimed = true;
                _fetchPromise = null;
            });
        }
        return _fetchPromise;
    }

function createStarRating(rating, interactive, onHover, onClick) {
    const container = document.createElement('div');
    container.className = 'star-rating';
    let currentSelectedRating = rating;
    
    for (let i = 1; i <= 5; i++) {
        const star = document.createElement('span');
        star.className = 'star';
        star.textContent = '\u2605';
        star.dataset.position = i;
        
        if (interactive) {
            star.addEventListener('mousemove', (e) => {
                const rect = star.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const isRightHalf = x > rect.width / 2;
                const hoverRating = (i - 1) * 2 + (isRightHalf ? 2 : 1);
                if (onHover) onHover(hoverRating);
            });
            star.addEventListener('click', (e) => {
                const rect = star.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const isRightHalf = x > rect.width / 2;
                currentSelectedRating = (i - 1) * 2 + (isRightHalf ? 2 : 1);
                if (onClick) onClick(currentSelectedRating);
            });
        }
        
        container.appendChild(star);
    }
    
    if (interactive) {
        container.addEventListener('mouseleave', () => {
            if (onHover) onHover(currentSelectedRating);
        });
    }
    
    updateStarDisplay(container, rating);
    return container;
}

function updateStarDisplay(container, rating) {
    const stars = container.querySelectorAll('.star');
    stars.forEach((star, index) => {
        const starPos = index + 1;
        star.classList.remove('filled', 'half');
        
        if (starPos <= Math.floor(rating / 2)) {
            star.classList.add('filled');
        } else if (starPos === Math.ceil(rating / 2) && rating % 2 === 1) {
            star.classList.add('half');
        }
    });
}

    async function loadRatings(itemId) {
        try {
            const response = await fetch(ApiClient.getUrl(`api/UserRatings/Item/${itemId}`), {
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            const data = await response.json();
            console.log('[UserRatings] Loaded ratings:', data);
            return data;
        } catch (error) {
            console.error('[UserRatings] Error loading ratings:', error);
            return { ratings: [], averageRating: 0, totalRatings: 0 };
        }
    }

    async function loadMyRating(itemId) {
        try {
            const userId = ApiClient.getCurrentUserId();
            const response = await fetch(ApiClient.getUrl(`api/UserRatings/MyRating/${itemId}?userId=${userId}`), {
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('[UserRatings] Error loading my rating:', error);
            return null;
        }
    }

    async function saveRating(itemId, rating, note) {
        try {
            const userId = ApiClient.getCurrentUserId();
            const user = await ApiClient.getCurrentUser();
            const userName = user ? user.Name : 'Unknown';
            const url = ApiClient.getUrl(`api/UserRatings/Rate?itemId=${itemId}&userId=${userId}&rating=${rating}${note ? '&note=' + encodeURIComponent(note) : ''}&userName=${encodeURIComponent(userName)}`);
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            
            if (!response.ok) {
                const text = await response.text();
                console.error('[UserRatings] Server error:', response.status, text);
                return { success: false, message: `Server error: ${response.status}` };
            }
            
            return await response.json();
        } catch (error) {
            console.error('[UserRatings] Error saving rating:', error);
            return { success: false, message: error.message };
        }
    }

    async function deleteRating(itemId) {
        try {
            const userId = ApiClient.getCurrentUserId();
            const url = ApiClient.getUrl(`api/UserRatings/Rating?itemId=${itemId}&userId=${userId}`);
            const response = await fetch(url, {
                method: 'DELETE',
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            return await response.json();
        } catch (error) {
            console.error('[UserRatings] Error deleting rating:', error);
            return { success: false, message: error.message };
        }
    }

    // ===== Phase 1: Hover-to-Rate =====

    let _popupCardElement = null;

    async function fetchUserRatings() {
        try {
            const userId = ApiClient.getCurrentUserId();
            const resp = await fetch(ApiClient.getUrl(`api/UserRatings/User/${userId}`), {
                headers: { 'X-Emby-Token': ApiClient.accessToken() }
            });
            if (!resp.ok) { userRatingsMap = {}; return; }
            const data = await resp.json();
            userRatingsMap = {};
            if (data.ratings && data.ratings.length > 0) {
                data.ratings.forEach(r => {
                    userRatingsMap[r.itemId] = { rating: r.rating, note: r.note };
                });
            }
        } catch (e) {
            console.error('[UserRatings] Error fetching user ratings:', e);
            userRatingsMap = {};
        }
    }

    function getUserRating(itemId) {
        return userRatingsMap && userRatingsMap[itemId] ? userRatingsMap[itemId] : null;
    }

    function formatStarRating(val) {
        var v10 = val * 2;
        return v10 === Math.floor(v10) ? v10 + '/10' : v10.toFixed(1) + '/10';
    }

    function avgCacheGet(itemId) {
        if (avgCache.has(itemId)) {
            const idx = avgCacheOrder.indexOf(itemId);
            if (idx > -1) avgCacheOrder.splice(idx, 1);
            avgCacheOrder.push(itemId);
            return avgCache.get(itemId);
        }
        return null;
    }

    function avgCacheSet(itemId, entry) {
        if (avgCache.has(itemId)) {
            const idx = avgCacheOrder.indexOf(itemId);
            if (idx > -1) avgCacheOrder.splice(idx, 1);
        } else if (avgCacheOrder.length >= AVG_CACHE_MAX) {
            const oldest = avgCacheOrder.shift();
            if (oldest !== undefined) avgCache.delete(oldest);
        }
        avgCache.set(itemId, entry);
        avgCacheOrder.push(itemId);
    }

    async function fetchBatchAverage(itemIds) {
        const ids = Array.from(itemIds);
        if (ids.length === 0) return {};
        try {
            const resp = await fetch(ApiClient.getUrl('api/UserRatings/BatchAverage'), {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Emby-Token': ApiClient.accessToken()
                },
                body: JSON.stringify({ itemIds: ids })
            });
            if (!resp.ok) return {};
            const data = await resp.json();
            const map = {};
            if (data.items) {
                Object.keys(data.items).forEach(k => {
                    const v = data.items[k];
                    map[k] = { averageRating: v.averageRating, totalRatings: v.totalRatings };
                    avgCacheSet(k, { averageRating: v.averageRating, totalRatings: v.totalRatings });
                });
            }
            ids.forEach(id => {
                if (!map[id]) {
                    map[id] = { averageRating: 0, totalRatings: 0 };
                    avgCacheSet(id, { averageRating: 0, totalRatings: 0 });
                }
            });
            return map;
        } catch (e) {
            console.error('[UserRatings] Error fetching batch averages:', e);
            return {};
        }
    }

    function fetchBatchAverageCoalesced(itemIds) {
        const ids = Array.from(new Set(itemIds));
        ids.forEach(id => _batchQueuedIds.add(id));

        if (_batchInFlight) {
            return _batchInFlight.then(() => {
                const stillQueued = Array.from(_batchQueuedIds);
                if (stillQueued.length === 0) return {};
                _batchQueuedIds = new Set();
                _batchInFlight = fetchBatchAverage(stillQueued);
                return _batchInFlight;
            });
        }

        _batchQueuedIds = new Set();
        _batchInFlight = fetchBatchAverage(ids);
        return _batchInFlight;
    }

    function buildAvgBadgeHtml(rating, count) {
        return '<div class="ur-avg-badge">' +
            '<span class="ur-avg-star">\u2605</span>' +
            '<span class="ur-avg-value">' + rating + '</span>' +
            '<span class="ur-avg-count">(' + count + ')</span>' +
        '</div>';
    }

    function decorateCard(card, info) {
        if (!card || card.getAttribute('data-ur-decorated') === '1') return;

        const scalable = card.querySelector('.cardScalable');
        if (!scalable) return;

        const hasAvg = info && info.averageRating && info.totalRatings > 0;
        if (hasAvg) {
            card.setAttribute('data-ur-has-avg', '1');
            const rating = info.averageRating.toFixed(1);
            if (!scalable.querySelector('.ur-avg-badge')) {
                scalable.insertAdjacentHTML('beforeend', buildAvgBadgeHtml(rating, info.totalRatings));
            }
        } else {
            card.removeAttribute('data-ur-has-avg');
        }

        let compact = scalable.querySelector('.compact-rating');
        const userRating = getUserRating(card.getAttribute('data-id'));
        if (userRating) {
            if (!compact) {
                compact = document.createElement('div');
                compact.className = 'compact-rating';
                compact.dataset.empty = 'false';
                compact.innerHTML = '<span class="cr-heart">\u2665</span><span class="cr-value"></span><span class="cr-edit">\u270E</span>';
                const imgContainer = scalable.querySelector('.cardImageContainer');
                if (imgContainer && imgContainer.parentNode) {
                    imgContainer.parentNode.insertBefore(compact, imgContainer);
                } else {
                    scalable.appendChild(compact);
                }
            }
            compact.querySelector('.cr-value').textContent = formatStarRating(userRating.rating / 2);
            compact.dataset.empty = 'false';
            compact.style.display = '';
            if (!compact._rateEditAttached) {
                compact._rateEditAttached = true;
                compact.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    const itemId = card.getAttribute('data-id');
                    const nameEl = card.querySelector('.cardText a');
                    const name = nameEl ? nameEl.textContent.trim() : null;
                    const existing = getUserRating(itemId);
                    const r = existing ? existing.rating / 2 : 0;
                    const note = existing ? (existing.note || '') : '';
                    _popupCardElement = card;
                    openRatePopup(itemId, name, r, note);
                });
            }
        } else if (compact) {
            compact.dataset.empty = 'true';
            compact.style.display = 'none';
        }

        card.setAttribute('data-ur-decorated', '1');
    }

    function decorateCardsIn(container, averagesMap) {
        const cards = container.querySelectorAll('.card[data-id]:not([data-ur-decorated])');
        if (!cards.length) return;

        const cached = {};
        const needFetch = [];
        cards.forEach(card => {
            const id = card.getAttribute('data-id');
            const c = avgCacheGet(id);
            if (c) {
                cached[id] = c;
            } else if (averagesMap && averagesMap[id]) {
                cached[id] = averagesMap[id];
            } else {
                needFetch.push(id);
            }
        });

        Object.keys(cached).forEach(id => decorateCardById(container, id, cached[id]));

        if (needFetch.length > 0) {
            fetchBatchAverageCoalesced(needFetch).then(map => {
                if (!map) return;
                Object.keys(map).forEach(id => decorateCardById(container, id, map[id]));
            });
        }
    }

    function decorateCardById(container, itemId, info) {
        const card = container.querySelector('.card[data-id="' + cssEscape(itemId) + '"]:not([data-ur-decorated])');
        if (card) decorateCard(card, info);
    }

    function cssEscape(s) {
        if (window.CSS && CSS.escape) return CSS.escape(s);
        return String(s).replace(/[^a-zA-Z0-9_-]/g, '\\$&');
    }

    function decorateAllCards() {
        ensureUserRatings().then(function() {
            _decorateCardsGlobal();
        });
    }

    function _decorateCardsGlobal() {
        decorateCardsIn(document.body, null);
    }

    function setPopupStarFill(stars, value) {
        stars.forEach(function(s, idx) {
            var starNum = idx + 1;
            var fillPct = 0;
            if (value >= starNum) {
                fillPct = 100;
            } else if (starNum - value === 0.5) {
                fillPct = 50;
            }
            s.style.setProperty('--star-fill', fillPct + '%');
        });
    }

    function createRatePopupModal() {
        if (popupModal) return popupModal;

        const overlay = document.createElement('div');
        overlay.className = 'rate-popup-overlay';
        overlay.id = 'ratePopupOverlay';
        overlay.innerHTML = `
            <div class="rate-popup">
                <div class="rate-popup-header">
                    <span class="rate-popup-title" id="rpTitle">Rate</span>
                    <button class="rate-popup-close" id="rpClose">&#xD7;</button>
                </div>
                <div class="rate-popup-subtitle" id="rpSubtitle">Select your rating</div>
                <div class="rate-popup-stars" id="rpStars">
                    <span class="rp-star" data-n="1">&#x2605;</span>
                    <span class="rp-star" data-n="2">&#x2605;</span>
                    <span class="rp-star" data-n="3">&#x2605;</span>
                    <span class="rp-star" data-n="4">&#x2605;</span>
                    <span class="rp-star" data-n="5">&#x2605;</span>
                </div>
                <textarea id="rpNote" placeholder="Optional note&#x2026;"></textarea>
                <div class="rate-popup-actions">
                    <button class="rate-popup-btn-delete" id="rpDelete" style="display:none;">Delete</button>
                    <div style="display:flex;gap:0.6em;margin-left:auto;">
                        <button class="rate-popup-btn-cancel" id="rpCancel">Cancel</button>
                        <button class="rate-popup-btn-submit" id="rpSubmit">Post Rating</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        popupModal = overlay;

        // Star hover/click in popup
        const popupStars = overlay.querySelectorAll('.rp-star');
        popupStars.forEach(function(star) {
            star.addEventListener('mouseenter', function(e) {
                var n = parseFloat(this.dataset.n);
                var rect = this.getBoundingClientRect();
                var isLeft = e.clientX < rect.left + rect.width / 2;
                var val = isLeft ? n - 0.5 : n;
                setPopupStarFill(popupStars, val);
                document.getElementById('rpSubtitle').textContent = 'Your rating: ' + formatStarRating(val);
                // Highlight hovered star
                popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
                this.classList.add('hover-highlight');
            });
            star.addEventListener('click', function(e) {
                var n = parseFloat(this.dataset.n);
                var rect = this.getBoundingClientRect();
                var isLeft = e.clientX < rect.left + rect.width / 2;
                var val = isLeft ? n - 0.5 : n;
                setPopupStarFill(popupStars, val);
                overlay.dataset.selectedRating = String(val);
                document.getElementById('rpSubtitle').textContent = 'Your rating: ' + formatStarRating(val);
                popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
            });
        });
        const popupStarsContainer = overlay.querySelector('#rpStars');
        popupStarsContainer.addEventListener('mouseleave', function() {
            var selected = parseFloat(overlay.dataset.selectedRating || '0');
            setPopupStarFill(popupStars, selected);
            popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
        });

        // Close handlers
        overlay.querySelector('#rpClose').addEventListener('click', closeRatePopup);
        overlay.querySelector('#rpCancel').addEventListener('click', closeRatePopup);
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) closeRatePopup();
        });

        // Submit handler
        overlay.querySelector('#rpSubmit').addEventListener('click', async function() {
            const itemId = popupActiveItemId;
            const selected = parseFloat(overlay.dataset.selectedRating || '0');
            if (selected === 0) return;

            const btn = this;
            btn.disabled = true;
            btn.textContent = 'Posting\u2026';

            const note = overlay.querySelector('#rpNote').value;
            const result = await saveRating(itemId, selected * 2, note);

            btn.disabled = false;
            btn.textContent = 'Post Rating';

            if (result.success) {
                const cardToAnimate = _popupCardElement;
                const wasEditing = popupModal.dataset.selectedRating !== '0' && (popupModal.querySelector('#rpDelete').style.display !== 'none');
                const isNewRating = !wasEditing;
                closeRatePopup();
                userRatingsMap[itemId] = { rating: selected * 2, note: note };

                if (cardToAnimate) {
                    animateRatingSuccess(cardToAnimate, selected);
                } else {
                    onDetailPageRatingChanged(itemId, true);
                }
            } else {
                alert('Error saving rating: ' + result.message);
            }
        });

        // Delete handler
        overlay.querySelector('#rpDelete').addEventListener('click', async function() {
            const itemId = popupActiveItemId;
            if (!confirm('Delete your rating?')) return;

            const btn = this;
            btn.disabled = true;
            btn.textContent = 'Deleting\u2026';

            const result = await deleteRating(itemId);

            btn.disabled = false;
            btn.textContent = 'Delete';

            if (result.success) {
                const cardToAnimate = _popupCardElement;
                closeRatePopup();
                if (userRatingsMap) delete userRatingsMap[itemId];

                if (cardToAnimate) {
                    animateRatingSuccess(cardToAnimate, 0);
                } else {
                    onDetailPageRatingChanged(itemId, false);
                }
            } else {
                alert('Error deleting rating: ' + result.message);
            }
        });

        return popupModal;
    }

    function openRatePopup(itemId, itemName, preselected, existingNote) {
        if (!popupModal) createRatePopupModal();
        popupActiveItemId = itemId;

        document.getElementById('rpTitle').textContent = 'Rate "' + (itemName || 'this item') + '"';
        document.getElementById('rpSubtitle').textContent = preselected > 0 ? 'Your rating: ' + formatStarRating(preselected) : 'Select your rating';
        document.getElementById('rpNote').value = existingNote || '';

        const popupStars = popupModal.querySelectorAll('.rp-star');
        setPopupStarFill(popupStars, preselected || 0);
        popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
        popupModal.dataset.selectedRating = String(preselected || 0);

        const deleteBtn = popupModal.querySelector('#rpDelete');
        const submitBtn = popupModal.querySelector('#rpSubmit');
        const isEditing = preselected > 0;
        deleteBtn.style.display = isEditing ? 'inline-block' : 'none';
        submitBtn.textContent = isEditing ? 'Update' : 'Post Rating';

        popupModal.classList.add('open');
    }

    function closeRatePopup() {
        if (popupModal) {
            popupModal.classList.remove('open');
            popupModal.dataset.selectedRating = '0';
        }
        popupActiveItemId = null;
        _popupCardElement = null;
    }

    function attachRateButtonListeners(container) {
        const buttons = container.querySelectorAll('.rate-badge');
        buttons.forEach(function(btn) {
            if (btn._rateBtnAttached) return;
            btn._rateBtnAttached = true;

            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const cardEl = btn.closest('.card');
                if (!cardEl) return;
                const itemId = cardEl.getAttribute('data-id');
                const nameEl = cardEl.querySelector('.cardText a');
                const name = nameEl ? nameEl.textContent.trim() : null;
                _popupCardElement = cardEl;
                openRatePopup(itemId, name, 0, null);
            });
        });
    }

    function animateRatingSuccess(card, rating) {
        const imageContainer = card.querySelector('.cardImageContainer');
        if (imageContainer) {
            imageContainer.classList.remove('card-rating-success');
            void imageContainer.offsetWidth;
            imageContainer.classList.add('card-rating-success');
            setTimeout(function() {
                imageContainer.classList.remove('card-rating-success');
            }, 700);
        }

        // Change "Rate" badge to "♥ N/10" on unrated cards (has rate-badge)
        const rateBadge = card.querySelector('.rate-badge');
        if (rateBadge) {
            rateBadge.innerHTML = '<span style="font-weight:600;font-size:0.9em;color:#e53935;">\u2665 ' + (rating * 2) + '/10</span>';
            rateBadge.classList.remove('rate-badge');
            rateBadge.removeAttribute('data-item-id');
        }

        // Show/update the compact rating badge
        let compactBadge = card.querySelector('.compact-rating');
        if (rating === 0) {
            // Deletion: hide the compact badge
            if (compactBadge) {
                compactBadge.dataset.empty = 'true';
                compactBadge.style.display = 'none';
            }
            return;
        }
        if (compactBadge) {
            compactBadge.querySelector('.cr-value').textContent = (rating * 2) + '/10';
            compactBadge.dataset.empty = 'false';
            compactBadge.style.display = '';
        } else {
            compactBadge = document.createElement('div');
            compactBadge.className = 'compact-rating';
            compactBadge.dataset.empty = 'false';
            compactBadge.innerHTML = '<span class="cr-heart">\u2665</span><span class="cr-value">' + (rating * 2) + '/10</span><span class="cr-edit">\u270E</span>';
            const imgContainer = card.querySelector('.cardImageContainer');
            if (imgContainer && imgContainer.parentNode) {
                imgContainer.parentNode.insertBefore(compactBadge, imgContainer);
            } else {
                card.querySelector('.cardScalable').appendChild(compactBadge);
            }
            if (!compactBadge._rateEditAttached) {
                compactBadge._rateEditAttached = true;
                compactBadge.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    const itemId = card.getAttribute('data-id');
                    const nameEl = card.querySelector('.cardText a');
                    const name = nameEl ? nameEl.textContent.trim() : null;
                    const existing = getUserRating(itemId);
                    const r = existing ? existing.rating / 2 : 0;
                    const note = existing ? (existing.note || '') : '';
                    _popupCardElement = card;
                    openRatePopup(itemId, name, r, note);
                });
            }
        }
    }

    function reInjectUI(itemId) {
        removeDetailPageUI();
        isInjecting = false;
        if (itemId) {
            setTimeout(() => injectRatingsUI(), 150);
        }
    }

    function removeDetailPageUI() {
        document.querySelectorAll('[data-ur-badge]').forEach(el => el.remove());
        const section = document.getElementById('urRatingsCollapsible');
        if (section) section.remove();
        closeRatingDetailsPopup();
    }

    // ===== DETAIL PAGE: BADGE NEXT TO IMDb RATING =====

    function buildDetailBadgeHtml(avg, totalRatings, myRating) {
        const myVal = myRating ? (myRating.rating / 2) : 0;
        const avgStr = avg > 0 ? avg.toFixed(1) : null;
        const starIcon = '<span class="ur-db-star">\u2605</span>';
        const heart = '<span class="ur-db-heart">\u2665</span>';
        if (myRating && myVal > 0) {
            if (avgStr) {
                return starIcon +
                    '<span class="ur-db-avg">' + avgStr + '</span>' +
                    '<span class="ur-db-sep">\u00B7</span>' +
                    heart +
                    '<span class="ur-db-mine">' + formatStarRating(myVal) + '</span>';
            }
            return heart +
                '<span class="ur-db-mine">' + formatStarRating(myVal) + '</span>';
        }
        if (avgStr) {
            return starIcon +
                '<span class="ur-db-avg">' + avgStr + '</span>' +
                '<span class="ur-db-sep">\u00B7</span>' +
                heart +
                '<span class="ur-db-rate">RATE</span>';
        }
        return heart +
            '<span class="ur-db-rate">RATE</span>';
    }

    function buildDetailBadgeTitle(avg, totalRatings, myRating) {
        const myVal = myRating ? (myRating.rating / 2) : 0;
        const avgStr = avg > 0 ? avg.toFixed(1) : null;
        const parts = [];
        if (avgStr) {
            parts.push('Community: ' + avgStr + ' (' + totalRatings + (totalRatings === 1 ? ' rating' : ' ratings') + ')');
        } else {
            parts.push('No community ratings');
        }
        if (myRating && myVal > 0) {
            parts.push('Your rating: ' + formatStarRating(myVal));
        } else {
            parts.push('Click to rate');
        }
        return parts.join(' \u00B7 ');
    }

    function updateDetailBadge(badge, avg, totalRatings, myRating) {
        badge.innerHTML = buildDetailBadgeHtml(avg, totalRatings, myRating);
        badge.dataset.urAvg = String(avg || 0);
        badge.dataset.urTotal = String(totalRatings || 0);
        badge.title = buildDetailBadgeTitle(avg, totalRatings, myRating);
    }

    async function injectRatingBadge(itemId, itemName) {
        const miscInfo = findVisibleDetail('.itemMiscInfo-primary');
        if (!miscInfo) return null;
        const starContainer = miscInfo.querySelector('.starRatingContainer');

        let badge = miscInfo.querySelector('[data-ur-badge]');
        if (!badge) {
            badge = document.createElement('div');
            badge.className = 'mediaInfoItem ur-detail-badge';
            badge.setAttribute('data-ur-badge', '1');
            if (starContainer) {
                starContainer.insertAdjacentElement('afterend', badge);
            } else {
                miscInfo.appendChild(badge);
            }
            badge.addEventListener('click', async function(e) {
                e.preventDefault();
                e.stopPropagation();
                const my = await loadMyRating(itemId);
                const r = my ? (my.rating / 2) : 0;
                const note = my ? (my.note || '') : '';
                _popupCardElement = null;
                openRatePopup(itemId, itemName, r, note);
            });
        }

        const myRating = await loadMyRating(itemId);
        let avg = 0, totalRatings = 0;
        try {
            const data = await loadRatings(itemId);
            avg = data.averageRating || 0;
            totalRatings = data.totalRatings || 0;
        } catch (e) { /* keep zeros */ }
        updateDetailBadge(badge, avg, totalRatings, myRating);
        return badge;
    }

    // ===== DETAIL PAGE: RATINGS SECTION AT BOTTOM (Cast/Similar style) =====

    function setUserAvatar(avatarEl, userId, userName) {
        var letter = (userName.charAt(0) || '?').toUpperCase();
        avatarEl.textContent = letter;
        if (userId) {
            var img = document.createElement('img');
            img.alt = userName || '';
            img.src = ApiClient.getUrl('Users/' + userId + '/Images/Primary',
                { maxwidth: 64, maxheight: 64, quality: 80 }) +
                '&api_key=' + ApiClient.accessToken();
            img.onload = function() {
                avatarEl.textContent = '';
                avatarEl.appendChild(img);
            };
            img.onerror = function() {
                avatarEl.textContent = letter;
            };
        }
    }

    function buildRatingCard(rating, currentUserId) {
        const userName = rating.userName || rating.UserName || 'Unknown User';
        const ratingValue = rating.rating || rating.Rating || 0;
        const noteText = rating.note || rating.Note || '';
        const timestamp = rating.timestamp || rating.Timestamp;
        const userId = rating.userId || rating.UserId || '';

        const card = document.createElement('div');
        card.className = 'ur-rating-card';
        if (userId && userId === currentUserId) {
            card.setAttribute('data-ur-mine', '1');
        }

        const header = document.createElement('div');
        header.className = 'ur-rc-header';

        const avatar = document.createElement('div');
        avatar.className = 'ur-rc-avatar';
        setUserAvatar(avatar, userId, userName);
        header.appendChild(avatar);

        const name = document.createElement('span');
        name.className = 'ur-rc-name';
        name.textContent = userName;
        header.appendChild(name);

        if (timestamp) {
            const date = document.createElement('span');
            date.className = 'ur-rc-date';
            const dateObj = new Date(timestamp);
            date.textContent = dateObj.toLocaleDateString(undefined, {
                year: 'numeric', month: 'short', day: 'numeric'
            });
            header.appendChild(date);
        }
        card.appendChild(header);

        const stars = document.createElement('div');
        stars.className = 'ur-rc-stars';
        var fullStars = Math.floor(ratingValue / 2);
        var hasHalf = ratingValue % 2 === 1;
        var emptyStars = 5 - fullStars - (hasHalf ? 1 : 0);
        stars.textContent = '\u2605'.repeat(fullStars) + (hasHalf ? '\u00BD' : '') + '\u2606'.repeat(emptyStars);
        card.appendChild(stars);

        if (noteText) {
            const comment = document.createElement('div');
            comment.className = 'ur-rc-comment';
            comment.textContent = noteText;
            card.appendChild(comment);
        }

        card.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            openRatingDetailsPopup({ userName: userName, userId: userId, rating: ratingValue, note: noteText, timestamp: timestamp });
        });
        return card;
    }

    async function injectRatingsSection(itemId) {
        let data;
        try { data = await loadRatings(itemId); }
        catch (e) { return; }
        const ratings = data.ratings || [];
        const totalRatings = data.totalRatings || 0;
        if (totalRatings === 0 || ratings.length === 0) return;

        const anchor = findVisibleDetail('#similarCollapsible') || findVisibleDetail('#castCollapsible');
        if (!anchor) return;

        let section = document.getElementById('urRatingsCollapsible');
        if (section) section.remove();
        section = document.createElement('div');
        section.id = 'urRatingsCollapsible';
        section.className = 'verticalSection detailVerticalSection verticalSection-extrabottompadding emby-scroller-container';

        const h2 = document.createElement('h2');
        h2.className = 'sectionTitle sectionTitle-cards padded-right';
        h2.textContent = 'Ratings';
        section.appendChild(h2);

        // Native-faithful scroll buttons (flat sibling after h2, not a nested row)
        const btnContainer = document.createElement('div');
        btnContainer.className = 'emby-scrollbuttons padded-right';
        const leftBtn = document.createElement('button');
        leftBtn.type = 'button';
        leftBtn.className = 'ur-scroll-btn emby-scrollbuttons-button paper-icon-button-light';
        leftBtn.setAttribute('data-direction', 'left');
        leftBtn.title = 'Previous';
        leftBtn.innerHTML = '<span class="material-icons chevron_left" aria-hidden="true"></span>';
        const rightBtn = document.createElement('button');
        rightBtn.type = 'button';
        rightBtn.className = 'ur-scroll-btn emby-scrollbuttons-button paper-icon-button-light';
        rightBtn.setAttribute('data-direction', 'right');
        rightBtn.title = 'Next';
        rightBtn.innerHTML = '<span class="material-icons chevron_right" aria-hidden="true"></span>';
        btnContainer.appendChild(leftBtn);
        btnContainer.appendChild(rightBtn);
        section.appendChild(btnContainer);

        // Plain scroller (no is="emby-scroller"/is="emby-itemscontainer")
        const scroller = document.createElement('div');
        scroller.className = 'ur-scroller';
        const itemsContainer = document.createElement('div');
        itemsContainer.className = 'ur-items';

        const currentUserId = ApiClient.getCurrentUserId();
        ratings.forEach(r => itemsContainer.appendChild(buildRatingCard(r, currentUserId)));
        scroller.appendChild(itemsContainer);
        section.appendChild(scroller);

        anchor.insertAdjacentElement('afterend', section);

        // Manual scroll + button disable logic
        function updateScrollButtons() {
            const maxScroll = scroller.scrollWidth - scroller.clientWidth;
            if (scroller.scrollLeft <= 2) {
                leftBtn.classList.add('is-disabled');
                leftBtn.disabled = true;
            } else {
                leftBtn.classList.remove('is-disabled');
                leftBtn.disabled = false;
            }
            if (scroller.scrollLeft >= maxScroll - 2) {
                rightBtn.classList.add('is-disabled');
                rightBtn.disabled = true;
            } else {
                rightBtn.classList.remove('is-disabled');
                rightBtn.disabled = false;
            }
        }
        leftBtn.addEventListener('click', function() {
            scroller.scrollBy({ left: -scroller.clientWidth * 0.8, behavior: 'smooth' });
        });
        rightBtn.addEventListener('click', function() {
            scroller.scrollBy({ left: scroller.clientWidth * 0.8, behavior: 'smooth' });
        });
        scroller.addEventListener('scroll', updateScrollButtons, { passive: true });
        window.addEventListener('resize', updateScrollButtons);
        // Initial state
        requestAnimationFrame(updateScrollButtons);
    }

    // ===== RATING DETAILS POPUP (read-only) =====

    let _detailsPopup = null;

    function openRatingDetailsPopup(rating) {
        if (!_detailsPopup) createRatingDetailsPopup();
        const userName = rating.userName || rating.UserName || 'Unknown User';
        const ratingValue = rating.rating || rating.Rating || 0;
        const noteText = rating.note || rating.Note || '';
        const timestamp = rating.timestamp || rating.Timestamp;
        const userId = rating.userId || rating.UserId || '';

        _detailsPopup.querySelector('.ur-rdp-name').textContent = userName;
        setUserAvatar(_detailsPopup.querySelector('.ur-rdp-avatar'), userId, userName);
        var fullStars = Math.floor(ratingValue / 2);
        var hasHalf = ratingValue % 2 === 1;
        var emptyStars = 5 - fullStars - (hasHalf ? 1 : 0);
        _detailsPopup.querySelector('.ur-rdp-stars').textContent = '\u2605'.repeat(fullStars) + (hasHalf ? '\u00BD' : '') + '\u2606'.repeat(emptyStars);
        const noteEl = _detailsPopup.querySelector('.ur-rdp-note');
        if (noteText) {
            noteEl.textContent = noteText;
            noteEl.style.display = '';
        } else {
            noteEl.style.display = 'none';
        }
        if (timestamp) {
            const dateObj = new Date(timestamp);
            _detailsPopup.querySelector('.ur-rdp-date').textContent = dateObj.toLocaleDateString(undefined, {
                year: 'numeric', month: 'long', day: 'numeric'
            });
        } else {
            _detailsPopup.querySelector('.ur-rdp-date').textContent = '';
        }
        _detailsPopup.classList.add('open');
    }

    function closeRatingDetailsPopup() {
        if (_detailsPopup) _detailsPopup.classList.remove('open');
    }

    function createRatingDetailsPopup() {
        if (_detailsPopup) return _detailsPopup;
        const overlay = document.createElement('div');
        overlay.className = 'ur-rating-details-overlay';
        overlay.innerHTML =
            '<div class="ur-rating-details-popup">' +
                '<div class="ur-rdp-header">' +
                    '<div class="ur-rdp-user">' +
                        '<div class="ur-rdp-avatar"></div>' +
                        '<div>' +
                            '<div class="ur-rdp-name"></div>' +
                            '<div class="ur-rdp-date"></div>' +
                        '</div>' +
                    '</div>' +
                    '<button class="ur-rdp-close">\u00D7</button>' +
                '</div>' +
                '<div class="ur-rdp-stars"></div>' +
                '<div class="ur-rdp-note"></div>' +
            '</div>';
        overlay.querySelector('.ur-rdp-close').addEventListener('click', closeRatingDetailsPopup);
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) closeRatingDetailsPopup();
        });
        document.body.appendChild(overlay);
        _detailsPopup = overlay;
        return overlay;
    }

    // ===== DETAIL-PAGE RATING CHANGED HOOK (no reload) =====

    function onDetailPageRatingChanged(itemId, saved) {
        if (currentItemId !== itemId) return;

        const badge = document.querySelector('[data-ur-badge]');
        if (badge) {
            const myRating = getUserRating(itemId);
            const avg = parseFloat(badge.dataset.urAvg || '0');
            const totalRatings = parseInt(badge.dataset.urTotal || '0', 10);
            updateDetailBadge(badge, avg, totalRatings, myRating);
        }

        let itemName = null;
        try {
            const nameEl = findVisibleDetail('.nameContainer');
            if (nameEl) itemName = nameEl.textContent.trim();
        } catch (e) {}

        if (saved) {
            injectRatingsSection(itemId).catch(() => {});
        } else {
            const section = document.getElementById('urRatingsCollapsible');
            if (section) {
                const myCard = section.querySelector('[data-ur-mine="1"]');
                if (myCard) myCard.remove();
                if (!section.querySelector('.ur-rating-card')) section.remove();
            }
        }
    }

    // ===== HELPERS: VISIBLE CONTAINER LOOKUP =====

    function isVisibleContainer(el) {
        if (!el) return false;
        let node = el;
        while (node) {
            if (node.classList && node.classList.contains('hide')) return false;
            node = node.parentElement;
        }
        return true;
    }

    function findVisibleDetail(selector) {
        const candidates = document.querySelectorAll(selector);
        return Array.from(candidates).find(isVisibleContainer) || null;
    }

    // ===== DETAIL-PAGE INJECTION ENTRYPOINT =====

    let injectionAttempts = 0;
    const maxInjectionAttempts = 30;

    function injectRatingsUI() {
        if (isInjecting) {
            console.log('[UserRatings] Already injecting, skipping');
            return;
        }

        let itemId = null;
        const urlParams = new URLSearchParams(window.location.search);
        itemId = urlParams.get('id');

        if (!itemId && window.location.hash.includes('?')) {
            const hashParams = new URLSearchParams(window.location.hash.split('?')[1]);
            itemId = hashParams.get('id');
        }

        if (!itemId) {
            const guidMatch = window.location.href.match(/([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})/i);
            if (guidMatch) itemId = guidMatch[1];
        }

        if (!itemId) {
            console.log('[UserRatings] No item ID found');
            injectionAttempts = 0;
            return;
        }

        if (currentItemId === itemId && document.querySelector('[data-ur-badge]')) {
            console.log('[UserRatings] Badge already exists for this item, skipping');
            injectionAttempts = 0;
            return;
        }

        if (currentItemId !== itemId) {
            removeDetailPageUI();
        }

        const miscInfo = findVisibleDetail('.itemMiscInfo-primary');
        if (!miscInfo || !miscInfo.querySelector('.starRatingContainer')) {
            if (injectionAttempts < maxInjectionAttempts) {
                injectionAttempts++;
                const retryDelay = Math.min(100 * Math.pow(1.5, injectionAttempts), 3000);
                console.log(`[UserRatings] Container not ready, retry ${injectionAttempts}/${maxInjectionAttempts} in ${retryDelay.toFixed(0)}ms`);
                setTimeout(injectRatingsUI, retryDelay);
            } else {
                console.log('[UserRatings] Max injection attempts reached, resetting for retry');
                injectionAttempts = 0;
                isInjecting = false;
            }
            return;
        }

        currentItemId = itemId;
        isInjecting = true;
        injectionAttempts = 0;

        let itemName = null;
        try {
            const nameEl = findVisibleDetail('.nameContainer');
            if (nameEl) itemName = nameEl.textContent.trim();
        } catch (e) {}

        Promise.all([
            injectRatingBadge(itemId, itemName).catch(e => console.error('[UserRatings] Badge inject error:', e)),
            injectRatingsSection(itemId).catch(e => console.error('[UserRatings] Section inject error:', e))
        ]).then(() => {
            setTimeout(() => {
                isInjecting = false;
                if (!document.querySelector('[data-ur-badge]') && currentItemId) {
                    console.log('[UserRatings] Badge missing after injection, retrying');
                    injectionAttempts = 0;
                    setTimeout(injectRatingsUI, 200);
                } else {
                    console.log('[UserRatings] ✓ Detail-page UI injected');
                }
            }, 200);
        }).catch(() => {
            isInjecting = false;
            injectionAttempts = 0;
        });
    }

    // Watch for page changes
    let lastUrl = location.href;
    new MutationObserver((mutations) => {
        const url = location.href;
        if (url !== lastUrl) {
            lastUrl = url;
            removeDetailPageUI();
            isInjecting = false;
            injectionAttempts = 0;
            currentItemId = null;
            setTimeout(injectRatingsUI, 150);
            return;
        }

        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node.nodeType !== 1) continue;
                if (node.classList && (
                    node.classList.contains('detailPagePrimaryContent') ||
                    node.classList.contains('itemDetailPage') ||
                    node.classList.contains('itemMiscInfo-primary') ||
                    node.classList.contains('starRatingContainer')
                )) {
                    if (!isInjecting) {
                        console.log('[UserRatings] Detail page container detected, triggering injection');
                        injectionAttempts = 0;
                        setTimeout(injectRatingsUI, 100);
                    }
                    return;
                }
                if (node.querySelector && (
                    node.querySelector('.detailPagePrimaryContent') ||
                    node.querySelector('.itemDetailPage') ||
                    node.querySelector('.itemMiscInfo-primary')
                )) {
                    if (!isInjecting) {
                        console.log('[UserRatings] Detail page content detected in mutation, triggering injection');
                        injectionAttempts = 0;
                        setTimeout(injectRatingsUI, 100);
                    }
                    return;
                }
            }
        }
    }).observe(document.body, { subtree: true, childList: true });

    // Initial injection
    setTimeout(injectRatingsUI, 100);
    setTimeout(injectRatingsUI, 300);
    setTimeout(injectRatingsUI, 600);

    // hashchange: clean up detail-page UI, restore home tab, re-inject
    window.addEventListener('hashchange', () => {
        removeDetailPageUI();

        const currentHash = window.location.hash;

        if (currentHash.includes('home')) {
            const wasUserRatings = window.history.state && window.history.state.userRatingsActive;
            if (wasUserRatings) {
                setTimeout(() => {
                    const tabBtn = document.querySelector('[data-ratings-tab="true"]');
                    if (tabBtn) {
                        document.querySelectorAll('.emby-tab-button').forEach(t => t.classList.remove('emby-tab-button-active'));
                        tabBtn.click();
                    }
                }, 100);
            }
        }

        isInjecting = false;
        injectionAttempts = 0;
        currentItemId = null;

        setTimeout(injectRatingsTab, 100);
        setTimeout(injectRatingsTab, 500);
        setTimeout(injectRatingsUI, 100);
        setTimeout(injectRatingsUI, 300);
        scheduleGlobalDecorate();
    });

    // Function to display ratings list in the home page content area
    async function displayRatingsList() {
        // Find the visible #indexPage — Jellyfin may have multiple cached copies,
        // only one is visible (no .hide class). Always create #ratingsTab fresh
        // inside the visible copy to avoid orphaning in a hidden cached page.
        const indexPage = document.querySelector('#indexPage:not(.hide)') || document.querySelector('#indexPage');
        if (!indexPage) {
            console.error('[UserRatings] Could not find #indexPage for ratings tab');
            return;
        }

        // Remove any existing #ratingsTab from ALL #indexPage copies (visible or hidden)
        document.querySelectorAll('#ratingsTab').forEach(tab => tab.remove());

        // Create fresh #ratingsTab inside the visible #indexPage
        const existingCount = indexPage.querySelectorAll('.tabContent.pageTabContent').length;
        const ratingsTabContent = document.createElement('div');
        ratingsTabContent.id = 'ratingsTab';
        ratingsTabContent.className = 'tabContent pageTabContent';
        ratingsTabContent.setAttribute('data-index', existingCount);
        indexPage.appendChild(ratingsTabContent);

        // Show loading (content population — visibility is managed by Jellyfin's .is-active)
        ratingsTabContent.innerHTML = '<div style="padding: 3em 2em; text-align: center; color: rgba(255,255,255,0.6);">Loading ratings...</div>';

        try {
            // Fetch user's ratings for compact badge display
            await ensureUserRatings();

            // Get config for page size (items per page in Rated Movies/Shows + Watched sections)
            let perPage = 24;
            try {
                const pluginConfig = await ApiClient.getPluginConfiguration('b8c5d3e7-4f6a-8b9c-1d2e-3f4a5b6c7d8e');
                perPage = pluginConfig.RecentlyRatedItemsCount || 10;
            } catch (error) {}

            function getItemCardImage(itemId, seriesId, itemType) {
                const imageId = itemType === 'Episode' && seriesId ? seriesId : itemId;
                return {
                    thumb: ApiClient.getImageUrl(imageId, { type: 'Thumb', fillWidth: 426, fillHeight: 240, quality: 80 }),
                    backdrop: ApiClient.getImageUrl(imageId, { type: 'Backdrop', fillWidth: 426, fillHeight: 240, quality: 80 }),
                    primary: ApiClient.getImageUrl(imageId, { type: 'Primary', fillWidth: 426, fillHeight: 240, quality: 80 })
                };
            }

            function applyImageFallback(el) {
                const thumb = el.getAttribute('data-thumb');
                const backdrop = el.getAttribute('data-backdrop');
                const primary = el.getAttribute('data-primary');
                const step = parseInt(el.getAttribute('data-fallback-step') || '0');
                const urls = [thumb, backdrop, primary];
                if (step < urls.length) {
                    const testImg = new Image();
                    testImg.onload = function() {
                        el.style.backgroundImage = "url('" + urls[step] + "')";
                    };
                    testImg.onerror = function() {
                        el.setAttribute('data-fallback-step', String(step + 1));
                        applyImageFallback(el);
                    };
                    testImg.src = urls[step];
                } else {
                    el.style.backgroundImage = 'none';
                }
            }

            // Function to build the ratings grid HTML for a category (backdrop cards in vertical-wrap)
            const buildCategoryGrid = (items) => items.map(item => {
                const details = item.details || item;
                const itemId = item.itemId || details.Id || details.itemId;
                const seriesId = details.SeriesId || details.seriesId;
                const itemType = details.Type || details.type;
                const urls = getItemCardImage(itemId, seriesId, itemType);
                const title = details.Name || details.name || 'Unknown';
                const serverId = ApiClient.serverId();

                return `
                    <div data-index="0" data-isfolder="false" data-serverid="${serverId}" data-id="${itemId}" data-type="${itemType}" data-mediatype="Video" class="card backdropCard card-hoverable card-withuserdata">
                        <div class="cardBox cardBox-bottompadded">
                            <div class="cardScalable">
                                <div class="cardPadder cardPadder-backdrop"></div>
                                <a href="#/details?id=${itemId}&serverId=${serverId}" data-action="link" class="cardImageContainer cardContent itemAction" aria-label="${title}" data-thumb="${urls.thumb}" data-backdrop="${urls.backdrop}" data-primary="${urls.primary}" data-fallback-step="0"></a>
                                <div class="cardIndicators cardIndicators-bottomright"></div>
                            </div>
                            <div class="cardText cardTextCentered cardText-first">
                                <bdi>
                                    <a href="#/details?id=${itemId}&serverId=${serverId}" data-id="${itemId}" data-serverid="${serverId}" data-type="${itemType}" data-action="link" class="itemAction textActionButton" title="${title}">${title}</a>
                                </bdi>
                            </div>
                            <div class="cardText cardTextCentered">&nbsp;</div>
                        </div>
                    </div>
                `;
            }).join('');

            // Function to build unrated item cards (backdrop cards with "Rate" badge)
            const buildUnratedGrid = (items) => items.map(item => {
                const serverId = ApiClient.serverId();
                const urls = getItemCardImage(item.itemId, item.seriesId, item.type);
                const title = item.name || 'Unknown';

                return `
                    <div data-index="0" data-isfolder="false" data-serverid="${serverId}" data-id="${item.itemId}" data-type="${item.type}" data-mediatype="Video" class="card backdropCard card-hoverable card-withuserdata">
                        <div class="cardBox cardBox-bottompadded">
                            <div class="cardScalable">
                                <div class="cardPadder cardPadder-backdrop"></div>
                                <a href="#/details?id=${item.itemId}&serverId=${serverId}" data-action="link" class="cardImageContainer cardContent itemAction" aria-label="${title}" data-thumb="${urls.thumb}" data-backdrop="${urls.backdrop}" data-primary="${urls.primary}" data-fallback-step="0"></a>
                                <div class="cardIndicators cardIndicators-bottomright">
                                    <div class="rate-badge" data-item-id="${item.itemId}">
                                        <span class="material-icons" style="font-size: 0.9em;">star_border</span>
                                        <span>Rate</span>
                                    </div>
                                </div>
                            </div>
                            <div class="cardText cardTextCentered cardText-first">
                                <bdi>
                                    <a href="#/details?id=${item.itemId}&serverId=${serverId}" data-id="${item.itemId}" data-serverid="${serverId}" data-type="${item.type}" data-action="link" class="itemAction textActionButton" title="${title}">${title}</a>
                                </bdi>
                            </div>
                            <div class="cardText cardTextCentered">&nbsp;</div>
                        </div>
                    </div>
                `;
            }).join('');

            const accessToken = ApiClient.accessToken();

            // Fetch a page of rated items from the server (server-side pagination)
            async function fetchRatedItemsPage(offset, limit, sortBy, sortDir, typeFilter) {
                const params = new URLSearchParams({ offset, limit, sortBy, sortDir });
                if (typeFilter && typeFilter !== 'all') params.set('typeFilter', typeFilter);
                const url = ApiClient.getUrl('api/UserRatings/AllRatedItems') + '?' + params.toString();
                const resp = await fetch(url, { headers: { 'X-Emby-Token': accessToken } });
                if (!resp.ok) return { items: [], total: 0 };
                const data = await resp.json();
                return { items: data.items || [], total: data.total || 0 };
            }

            // Normalize server item to the shape expected by buildCategoryGrid
            function normalizeRatedItem(item) {
                return {
                    ...item,
                    lastRatedTimestamp: item.lastRated ? new Date(item.lastRated).getTime() : 0,
                    details: {
                        Name: item.name,
                        Type: item.type,
                        SeriesId: item.seriesId,
                        Id: item.itemId
                    }
                };
            }

            // ===== Movies section state =====
            let movies_currentPage = 1;
            let movies_sortField = 'recent';
            let movies_sortDir = 'desc';
            let movies_total = 0;

            // ===== Shows section state =====
            let shows_currentPage = 1;
            let shows_sortField = 'recent';
            let shows_sortDir = 'desc';
            let shows_typeFilter = 'Series,Episode';
            let shows_total = 0;

            // Build sections HTML — Movies + Shows placeholders, then unrated placeholders
            let sectionsHTML = '<div>';
            sectionsHTML += '<div id="moviesSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);"><span class="material-icons" style="vertical-align: middle; margin-right: 0.3em;">hourglass_empty</span>Loading rated movies...</div></div>';
            sectionsHTML += '<div id="showsSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);"><span class="material-icons" style="vertical-align: middle; margin-right: 0.3em;">hourglass_empty</span>Loading rated shows...</div></div>';
            sectionsHTML += '</div>';
            sectionsHTML += '<div id="unratedMoviesSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">Loading watched movies...</div></div>';
            sectionsHTML += '<div id="unratedSeriesSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);"><span class="material-icons" style="vertical-align: middle; margin-right: 0.3em;">hourglass_empty</span>Loading watched shows...<br><span style="font-size: 0.85em; opacity: 0.7;">This may take a few seconds</span></div></div>';

            // Display the page immediately
            ratingsTabContent.innerHTML = sectionsHTML;

            // Initialize image fallbacks for all cards
            ratingsTabContent.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);

            // ===== Render Movies section (server-side paginated, sort + direction) =====
            const moviesSection = document.querySelector('#moviesSection');

            async function renderMoviesSection(page) {
                const offset = (page - 1) * perPage;
                const { items, total } = await fetchRatedItemsPage(
                    offset, perPage, movies_sortField, movies_sortDir, 'Movie'
                );
                movies_total = total;
                const pageItems = items.map(normalizeRatedItem);

                const totalPages = Math.max(1, Math.ceil(total / perPage));
                const startIndex = offset + 1;
                const endIndex = Math.min(offset + perPage, total);
                const dirArrow = movies_sortDir === 'desc' ? 'arrow_downward' : 'arrow_upward';

                moviesSection.innerHTML = `
                    <div class="verticalSection">
                        <div class="sectionTitleContainer sectionTitleContainer-cards padded-left">
                            <h2 class="sectionTitle sectionTitle-cards">Rated Movies</h2>
                        </div>
                        <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x" style="gap: 1em;">
                            <div class="paging">
                                <div class="listPaging">
                                    <span style="vertical-align:middle;">${total > 0 ? startIndex + '-' + endIndex : 0} of ${total}</span>
                                    <div style="display:inline-block;">
                                        <button is="paper-icon-button-light" class="prevPageBtn autoSize paper-icon-button-light" ${page === 1 ? 'disabled' : ''}>
                                            <span class="material-icons arrow_back" aria-hidden="true"></span>
                                        </button>
                                        <button is="paper-icon-button-light" class="nextPageBtn autoSize paper-icon-button-light" ${page >= totalPages ? 'disabled' : ''}>
                                            <span class="material-icons arrow_forward" aria-hidden="true"></span>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <select is="emby-select" class="sortSelect emby-select-withcolor emby-select" style="width: auto;">
                                <option value="rating" ${movies_sortField === 'rating' ? 'selected' : ''}>Rating</option>
                                <option value="title" ${movies_sortField === 'title' ? 'selected' : ''}>Title</option>
                                <option value="recent" ${movies_sortField === 'recent' ? 'selected' : ''}>Recently Rated</option>
                                <option value="count" ${movies_sortField === 'count' ? 'selected' : ''}>Most Ratings</option>
                            </select>
                            <button is="paper-icon-button-light" class="sortDirBtn autoSize paper-icon-button-light" title="Toggle sort direction">
                                <span class="material-icons ${dirArrow}" aria-hidden="true"></span>
                            </button>
                        </div>
                        <div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">
                            ${pageItems.length > 0 ? buildCategoryGrid(pageItems) : '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">No rated movies found.</div>'}
                        </div>
                    </div>
                `;

                moviesSection.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);
                const moviesAvgMap = {};
                pageItems.forEach(it => {
                    if (it.itemId) moviesAvgMap[it.itemId] = { averageRating: it.averageRating, totalRatings: it.totalRatings };
                });
                decorateCardsIn(moviesSection, moviesAvgMap);

                const sortSelect = moviesSection.querySelector('.sortSelect');
                if (sortSelect) {
                    sortSelect.addEventListener('change', (e) => {
                        movies_sortField = e.target.value;
                        movies_currentPage = 1;
                        renderMoviesSection(movies_currentPage);
                    });
                }

                const sortDirBtn = moviesSection.querySelector('.sortDirBtn');
                if (sortDirBtn) {
                    sortDirBtn.addEventListener('click', () => {
                        movies_sortDir = movies_sortDir === 'desc' ? 'asc' : 'desc';
                        movies_currentPage = 1;
                        renderMoviesSection(movies_currentPage);
                    });
                }

                const prevBtn = moviesSection.querySelector('.prevPageBtn');
                if (prevBtn && !prevBtn.disabled) {
                    prevBtn.addEventListener('click', () => {
                        movies_currentPage--;
                        renderMoviesSection(movies_currentPage);
                    });
                }

                const nextBtn = moviesSection.querySelector('.nextPageBtn');
                if (nextBtn && !nextBtn.disabled) {
                    nextBtn.addEventListener('click', () => {
                        movies_currentPage++;
                        renderMoviesSection(movies_currentPage);
                    });
                }
            }

            // ===== Render Shows section (server-side paginated, sort + direction + tab sub-filter) =====
            const showsSection = document.querySelector('#showsSection');

            async function renderShowsSection(page) {
                const offset = (page - 1) * perPage;
                const { items, total } = await fetchRatedItemsPage(
                    offset, perPage, shows_sortField, shows_sortDir, shows_typeFilter
                );
                shows_total = total;
                const pageItems = items.map(normalizeRatedItem);

                const totalPages = Math.max(1, Math.ceil(total / perPage));
                const startIndex = offset + 1;
                const endIndex = Math.min(offset + perPage, total);
                const dirArrow = shows_sortDir === 'desc' ? 'arrow_downward' : 'arrow_upward';

                // Tab sub-filter: All (Series+Episode) / Shows (Series) / Episodes (Episode)
                const tabAll = shows_typeFilter === 'Series,Episode';
                const tabShows = shows_typeFilter === 'Series';
                const tabEpisodes = shows_typeFilter === 'Episode';

                const typeTabsHtml = `
                    <div class="showsTypeTabs" style="display:inline-flex;gap:0;">
                        <button is="emby-button" class="typeTabBtn emby-button ${tabAll ? 'typeTabActive' : ''}" data-type="all" style="padding:0.5em 1em;font-size:0.85em;border-radius:4px 0 0 4px;background:${tabAll ? 'rgba(255,255,255,0.15)' : 'transparent'};color:${tabAll ? '#fff' : 'rgba(255,255,255,0.5)'};border:none;cursor:pointer;">All</button>
                        <button is="emby-button" class="typeTabBtn emby-button ${tabShows ? 'typeTabActive' : ''}" data-type="Series" style="padding:0.5em 1em;font-size:0.85em;border-radius:0;background:${tabShows ? 'rgba(255,255,255,0.15)' : 'transparent'};color:${tabShows ? '#fff' : 'rgba(255,255,255,0.5)'};border:none;border-left:1px solid rgba(255,255,255,0.1);cursor:pointer;">Shows</button>
                        <button is="emby-button" class="typeTabBtn emby-button ${tabEpisodes ? 'typeTabActive' : ''}" data-type="Episode" style="padding:0.5em 1em;font-size:0.85em;border-radius:0 4px 4px 0;background:${tabEpisodes ? 'rgba(255,255,255,0.15)' : 'transparent'};color:${tabEpisodes ? '#fff' : 'rgba(255,255,255,0.5)'};border:none;border-left:1px solid rgba(255,255,255,0.1);cursor:pointer;">Episodes</button>
                    </div>`;

                showsSection.innerHTML = `
                    <div class="verticalSection">
                        <div class="sectionTitleContainer sectionTitleContainer-cards padded-left">
                            <h2 class="sectionTitle sectionTitle-cards">Rated Shows</h2>
                        </div>
                        <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x" style="gap: 1em;">
                            <div class="paging">
                                <div class="listPaging">
                                    <span style="vertical-align:middle;">${total > 0 ? startIndex + '-' + endIndex : 0} of ${total}</span>
                                    <div style="display:inline-block;">
                                        <button is="paper-icon-button-light" class="prevPageBtn autoSize paper-icon-button-light" ${page === 1 ? 'disabled' : ''}>
                                            <span class="material-icons arrow_back" aria-hidden="true"></span>
                                        </button>
                                        <button is="paper-icon-button-light" class="nextPageBtn autoSize paper-icon-button-light" ${page >= totalPages ? 'disabled' : ''}>
                                            <span class="material-icons arrow_forward" aria-hidden="true"></span>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            ${typeTabsHtml}
                            <select is="emby-select" class="sortSelect emby-select-withcolor emby-select" style="width: auto;">
                                <option value="rating" ${shows_sortField === 'rating' ? 'selected' : ''}>Rating</option>
                                <option value="title" ${shows_sortField === 'title' ? 'selected' : ''}>Title</option>
                                <option value="recent" ${shows_sortField === 'recent' ? 'selected' : ''}>Recently Rated</option>
                                <option value="count" ${shows_sortField === 'count' ? 'selected' : ''}>Most Ratings</option>
                            </select>
                            <button is="paper-icon-button-light" class="sortDirBtn autoSize paper-icon-button-light" title="Toggle sort direction">
                                <span class="material-icons ${dirArrow}" aria-hidden="true"></span>
                            </button>
                        </div>
                        <div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">
                            ${pageItems.length > 0 ? buildCategoryGrid(pageItems) : '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">No rated shows found.</div>'}
                        </div>
                    </div>
                `;

                showsSection.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);
                const showsAvgMap = {};
                pageItems.forEach(it => {
                    if (it.itemId) showsAvgMap[it.itemId] = { averageRating: it.averageRating, totalRatings: it.totalRatings };
                });
                decorateCardsIn(showsSection, showsAvgMap);

                const sortSelect = showsSection.querySelector('.sortSelect');
                if (sortSelect) {
                    sortSelect.addEventListener('change', (e) => {
                        shows_sortField = e.target.value;
                        shows_currentPage = 1;
                        renderShowsSection(shows_currentPage);
                    });
                }

                const sortDirBtn = showsSection.querySelector('.sortDirBtn');
                if (sortDirBtn) {
                    sortDirBtn.addEventListener('click', () => {
                        shows_sortDir = shows_sortDir === 'desc' ? 'asc' : 'desc';
                        shows_currentPage = 1;
                        renderShowsSection(shows_currentPage);
                    });
                }

                const typeTabBtns = showsSection.querySelectorAll('.typeTabBtn');
                typeTabBtns.forEach(function(btn) {
                    btn.addEventListener('click', function(e) {
                        e.preventDefault();
                        const t = this.getAttribute('data-type');
                        if (t === 'all') shows_typeFilter = 'Series,Episode';
                        else if (t === 'Series') shows_typeFilter = 'Series';
                        else if (t === 'Episode') shows_typeFilter = 'Episode';
                        shows_currentPage = 1;
                        renderShowsSection(shows_currentPage);
                    });
                });

                const prevBtn = showsSection.querySelector('.prevPageBtn');
                if (prevBtn && !prevBtn.disabled) {
                    prevBtn.addEventListener('click', () => {
                        shows_currentPage--;
                        renderShowsSection(shows_currentPage);
                    });
                }

                const nextBtn = showsSection.querySelector('.nextPageBtn');
                if (nextBtn && !nextBtn.disabled) {
                    nextBtn.addEventListener('click', () => {
                        shows_currentPage++;
                        renderShowsSection(shows_currentPage);
                    });
                }
            }

            // Render Movies + Shows sections (async, non-blocking)
            renderMoviesSection(movies_currentPage).catch(e => {
                console.error('[UserRatings] Error loading movies section:', e);
                if (moviesSection) moviesSection.innerHTML = '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">Error loading rated movies.</div>';
            });
            renderShowsSection(shows_currentPage).catch(e => {
                console.error('[UserRatings] Error loading shows section:', e);
                if (showsSection) showsSection.innerHTML = '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">Error loading rated shows.</div>';
            });

            // Fetch unrated items via server-side endpoint
            const userId = ApiClient.getCurrentUserId();

            async function fetchUnratedType(itemType) {
                try {
                    const url = ApiClient.getUrl(`api/UserRatings/UnratedWatchedItems?userId=${userId}&itemType=${itemType}`);
                    const resp = await fetch(url, { headers: { 'X-Emby-Token': accessToken } });
                    if (!resp.ok) return [];
                    const data = await resp.json();
                    if (!data.items) return [];
                    return data.items.slice(0, 24);
                } catch (e) {
                    console.error('[UserRatings] Error fetching unrated items:', e);
                    return [];
                }
            }

            // Fetch unrated sections. Movies (~30ms) arrive fast, Series (~5s) is slow.
            Promise.all([
                fetchUnratedType('Movie'),
                fetchUnratedType('Series')
            ]).then(([unratedMoviesList, unratedSeriesList]) => {
                const unratedMoviesSection = document.querySelector('#unratedMoviesSection');
                if (unratedMoviesSection) {
                    if (unratedMoviesList.length > 0) {
                        const mapped = unratedMoviesList.map((item, idx) => ({
                            itemId: item.itemId,
                            name: item.name,
                            type: 'Movie',
                            averageRating: 0,
                            totalRatings: 0,
                            lastPlayedDate: item.lastPlayedDate || null,
                            _sortOrder: idx
                        }));
                        renderUnratedSection(unratedMoviesSection, mapped, 'Watched Movies — Not Yet Rated');
                    } else {
                        unratedMoviesSection.innerHTML = '';
                    }
                }

                const unratedSeriesSection = document.querySelector('#unratedSeriesSection');
                if (unratedSeriesSection) {
                    if (unratedSeriesList.length > 0) {
                        const mapped = unratedSeriesList.map((item, idx) => ({
                            itemId: item.itemId,
                            name: item.name,
                            type: 'Series',
                            averageRating: 0,
                            totalRatings: 0,
                            lastPlayedDate: item.lastPlayedDate || null,
                            _sortOrder: idx
                        }));
                        renderUnratedSection(unratedSeriesSection, mapped, 'Watched Shows — Not Yet Rated');
                    } else {
                        unratedSeriesSection.innerHTML = '';
                    }
                }
            }).then(() => {
                const tab = document.querySelector('#ratingsTab');
                if (tab) {
                    decorateCardsIn(tab, null);
                    attachRateButtonListeners(tab);
                }
            });

            // Helper: render an unrated section with client-side pagination (small fixed set, no server call)
            function renderUnratedSection(container, items, title) {
                let unratedPage = 1;
                let unratedSortDir = 'desc';
                let unratedSortField = 'watched';

                const sortUnrated = (arr, field, dir) => {
                    const d = dir === 'asc' ? 1 : -1;
                    if (field === 'watched') {
                        arr.sort((a, b) => {
                            if (a.lastPlayedDate && b.lastPlayedDate) return d === -1
                                ? b.lastPlayedDate.localeCompare(a.lastPlayedDate)
                                : a.lastPlayedDate.localeCompare(b.lastPlayedDate);
                            if (a.lastPlayedDate) return -1;
                            if (b.lastPlayedDate) return 1;
                            return (a._sortOrder || 0) - (b._sortOrder || 0);
                        });
                    } else if (field === 'title') {
                        arr.sort((a, b) => (a.name || '').localeCompare(b.name || '') * d);
                    }
                };

                sortUnrated(items, unratedSortField, unratedSortDir);

                function renderPage(page) {
                    const totalPages = Math.max(1, Math.ceil(items.length / perPage));
                    const startIndex = (page - 1) * perPage;
                    const endIndex = Math.min(startIndex + perPage, items.length);
                    const paginatedItems = items.slice(startIndex, endIndex);
                    const dirArrow = unratedSortDir === 'desc' ? 'arrow_downward' : 'arrow_upward';

                    container.innerHTML = `
                        <div class="verticalSection">
                            <div class="sectionTitleContainer sectionTitleContainer-cards padded-left">
                                <h2 class="sectionTitle sectionTitle-cards">${title}</h2>
                            </div>
                            <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x" style="gap: 1em;">
                                <div class="paging">
                                    <div class="listPaging">
                                        <span style="vertical-align:middle;">${items.length > 0 ? (startIndex + 1) + '-' + endIndex : 0} of ${items.length}</span>
                                        <div style="display:inline-block;">
                                            <button is="paper-icon-button-light" class="prevPageBtn autoSize paper-icon-button-light" ${page === 1 ? 'disabled' : ''}>
                                                <span class="material-icons arrow_back" aria-hidden="true"></span>
                                            </button>
                                            <button is="paper-icon-button-light" class="nextPageBtn autoSize paper-icon-button-light" ${page >= totalPages ? 'disabled' : ''}>
                                                <span class="material-icons arrow_forward" aria-hidden="true"></span>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                                <select is="emby-select" class="sortSelect emby-select-withcolor emby-select" style="width: auto;">
                                    <option value="watched" ${unratedSortField === 'watched' ? 'selected' : ''}>Last Watched</option>
                                    <option value="title" ${unratedSortField === 'title' ? 'selected' : ''}>Title</option>
                                </select>
                                <button is="paper-icon-button-light" class="sortDirBtn autoSize paper-icon-button-light" title="Toggle sort direction">
                                    <span class="material-icons ${dirArrow}" aria-hidden="true"></span>
                                </button>
                            </div>
                            <div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">
                                ${buildUnratedGrid(paginatedItems)}
                            </div>
                        </div>
                    `;

                    container.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);
                    decorateCardsIn(container, null);
                    attachRateButtonListeners(container);

                    const sortSel = container.querySelector('.sortSelect');
                    if (sortSel) {
                        sortSel.addEventListener('change', (e) => {
                            unratedSortField = e.target.value;
                            unratedSortDir = 'desc';
                            sortUnrated(items, unratedSortField, unratedSortDir);
                            unratedPage = 1;
                            renderPage(1);
                        });
                    }

                    const sdb = container.querySelector('.sortDirBtn');
                    if (sdb) {
                        sdb.addEventListener('click', () => {
                            unratedSortDir = unratedSortDir === 'desc' ? 'asc' : 'desc';
                            sortUnrated(items, unratedSortField, unratedSortDir);
                            unratedPage = 1;
                            renderPage(1);
                        });
                    }

                    const pb = container.querySelector('.prevPageBtn');
                    if (pb && !pb.disabled) {
                        pb.addEventListener('click', () => {
                            unratedPage--;
                            renderPage(unratedPage);
                        });
                    }

                    const nb = container.querySelector('.nextPageBtn');
                    if (nb && !nb.disabled) {
                        nb.addEventListener('click', () => {
                            unratedPage++;
                            renderPage(unratedPage);
                        });
                    }
                }

                renderPage(1);
            }

        } catch (error) {
            console.error('[UserRatings] Error displaying ratings list:', error);
            ratingsTabContent.innerHTML = `
                <div style="padding: 2em; background: rgba(229, 57, 53, 0.2); border: 1px solid rgba(229, 57, 53, 0.5); border-radius: 8px; color: #ff6b6b; margin: 2em;">
                    <strong>Error:</strong> ${error.message}
                </div>
            `;
        }
    }

    // Inject ratings tab on home screen
    function injectRatingsTab() {
        try {
            // Only inject on home page
            if (!window.location.hash.includes('home')) {
                return;
            }
            
            // Check if tab button already exists
            const existingTab = document.querySelector('[data-ratings-tab="true"]');
            if (existingTab) {
                // Ensure #ratingsTab content div exists inside the visible #indexPage
                ensureRatingsTabContent();
                return;
            }

            // Try to find the tabs container by locating the Home button first
            const homeButton = Array.from(document.querySelectorAll('.emby-tab-button')).find(btn => 
                btn.textContent.trim().toLowerCase().includes('home')
            );
            
            let tabsSlider = null;
            
            if (homeButton) {
                tabsSlider = homeButton.parentElement;
            } else {
                // Strategy 2: Look for .emby-tabs-slider
                tabsSlider = document.querySelector('.emby-tabs-slider');
            }
            
            if (!tabsSlider) {
                return;
            }

            // Get the next index
            const existingTabs = tabsSlider.querySelectorAll('.emby-tab-button');
            const nextIndex = existingTabs.length;

            // Create the ratings tab button
            const ratingsTab = document.createElement('button');
            ratingsTab.type = 'button';
            ratingsTab.setAttribute('is', 'emby-button');
            ratingsTab.className = 'emby-tab-button emby-button';
            ratingsTab.setAttribute('data-index', nextIndex);
            ratingsTab.setAttribute('data-ratings-tab', 'true');
            ratingsTab.innerHTML = '<div class="emby-button-foreground">User Ratings</div>';

            // Add click handler — DON'T call e.preventDefault().
            // Let Jellyfin's own tab handler manage .is-active on .pageTabContent and
            // .emby-tab-button-active on buttons. We just populate the content.
            ratingsTab.addEventListener('click', async function() {
                // Skip if already active AND #ratingsTab is visible (not orphaned in a
                // hidden cached #indexPage). If the button is active but #ratingsTab is
                // not visible, we must re-create it.
                if (ratingsTab.classList.contains('emby-tab-button-active')) {
                    const rt = document.querySelector('#ratingsTab');
                    const visibleIndex = document.querySelector('#indexPage:not(.hide)');
                    if (rt && visibleIndex && visibleIndex.contains(rt)) {
                        return; // Already active and visible — no refetch needed
                    }
                }

                try {
                    // Load and display ratings list (always creates fresh #ratingsTab
                    // inside the visible #indexPage)
                    await displayRatingsList();
                    // Annotate current history entry so back-button restores us
                    try {
                        const state = window.history.state || {};
                        state.userRatingsActive = true;
                        window.history.replaceState(state, '', window.location.href);
                    } catch (err) {
                        console.warn('[UserRatings] Could not replaceState on tab click:', err);
                    }
                } catch (error) {
                    console.error('[UserRatings] Error in displayRatingsList:', error);
                }
            });

            // Insert the tab button
            tabsSlider.appendChild(ratingsTab);

            // Create #ratingsTab as a real .tabContent.pageTabContent inside #indexPage
            ensureRatingsTabContent();

            // Listen to native tab clicks (capture phase) to clear userRatingsActive.
            // No DOM manipulation — just history state cleanup so back-navigation
            // restores the last-clicked native tab instead of User Ratings.
            const nativeTabs = tabsSlider.querySelectorAll('.emby-tab-button:not([data-ratings-tab="true"])');
            nativeTabs.forEach(tab => {
                tab.addEventListener('click', function() {
                    try {
                        const state = window.history.state || {};
                        if (state.userRatingsActive) {
                            delete state.userRatingsActive;
                            window.history.replaceState(state, '', window.location.href);
                        }
                    } catch (err) { /* ignore */ }
                }, true);
            });
            
        } catch (error) {
            console.error('[UserRatings] Tab injection error:', error);
        }
    }

    // Create #ratingsTab as a .tabContent.pageTabContent inside the visible #indexPage.
    // Removes any orphaned #ratingsTab from hidden/cached #indexPage copies first.
    function ensureRatingsTabContent() {
        const indexPage = document.querySelector('#indexPage:not(.hide)') || document.querySelector('#indexPage');
        if (!indexPage) return;

        // Check if #ratingsTab already exists inside the visible #indexPage
        const existing = indexPage.querySelector('#ratingsTab');
        if (existing) return;

        // Remove any orphaned #ratingsTab from other (hidden) #indexPage copies
        document.querySelectorAll('#ratingsTab').forEach(tab => tab.remove());

        // Create fresh inside the visible #indexPage
        const existingCount = indexPage.querySelectorAll('.tabContent.pageTabContent').length;
        const ratingsTabContent = document.createElement('div');
        ratingsTabContent.id = 'ratingsTab';
        ratingsTabContent.className = 'tabContent pageTabContent';
        ratingsTabContent.setAttribute('data-index', existingCount);
        indexPage.appendChild(ratingsTabContent);
        console.log('[UserRatings] Created #ratingsTab as pageTabContent inside visible #indexPage');
    }

    // On fresh page load (browser refresh), surrender to Jellyfin's default:
    // let the Home tab be active. Clear userRatingsActive from history state so
    // nothing restores the User Ratings tab on refresh. In-app navigation
    // (back from details) re-sets this flag via the tab click handler, so that
    // flow is unaffected.
    try {
        if (window.history.state && window.history.state.userRatingsActive) {
            const state = window.history.state;
            delete state.userRatingsActive;
            window.history.replaceState(state, '', window.location.href);
            console.log('[UserRatings] Fresh page load — cleared userRatingsActive, Home tab wins');
        }
    } catch (err) { /* ignore */ }

    // Try immediately and repeatedly
    injectRatingsTab();
    setTimeout(injectRatingsTab, 100);
    setTimeout(injectRatingsTab, 500);
    setTimeout(injectRatingsTab, 1000);
    setTimeout(injectRatingsTab, 2000);
    setTimeout(injectRatingsTab, 3000);
    setInterval(injectRatingsTab, 2000);

    // Prime user ratings cache at boot so global card decoration can fill compact badges
    ensureUserRatings().then(function() {
        // Re-pass: add personal badges to cards decorated before userRatingsMap was ready
        document.querySelectorAll('.card[data-ur-decorated="1"]').forEach(function(card) {
            const scalable = card.querySelector('.cardScalable');
            if (!scalable) return;
            const compact = scalable.querySelector('.compact-rating');
            const itemId = card.getAttribute('data-id');
            const userRating = getUserRating(itemId);
            if (userRating) {
                if (!compact) {
                    const c = document.createElement('div');
                    c.className = 'compact-rating';
                    c.dataset.empty = 'false';
                    c.innerHTML = '<span class="cr-heart">\u2665</span><span class="cr-value"></span><span class="cr-edit">\u270E</span>';
                    c.querySelector('.cr-value').textContent = formatStarRating(userRating.rating / 2);
                    const imgContainer = scalable.querySelector('.cardImageContainer');
                    if (imgContainer && imgContainer.parentNode) {
                        imgContainer.parentNode.insertBefore(c, imgContainer);
                    } else {
                        scalable.appendChild(c);
                    }
                    if (!c._rateEditAttached) {
                        c._rateEditAttached = true;
                        c.addEventListener('click', function(e) {
                            e.preventDefault();
                            e.stopPropagation();
                            const id = card.getAttribute('data-id');
                            const nameEl = card.querySelector('.cardText a');
                            const name = nameEl ? nameEl.textContent.trim() : null;
                            const existing = getUserRating(id);
                            const r = existing ? existing.rating / 2 : 0;
                            const note = existing ? (existing.note || '') : '';
                            _popupCardElement = card;
                            openRatePopup(id, name, r, note);
                        });
                    }
                } else if (compact.dataset.empty === 'true') {
                    compact.querySelector('.cr-value').textContent = formatStarRating(userRating.rating / 2);
                    compact.dataset.empty = 'false';
                    compact.style.display = '';
                }
            }
        });
    });

    // Global card decoration — decorate cards across all Jellyfin surfaces (library, home, collections, search)
    function scheduleGlobalDecorate() {
        if (_decorateTimer) return;
        _decorateTimer = setTimeout(function() {
            _decorateTimer = null;
            decorateAllCards();
        }, 150);
    }

    decorateAllCards();
    setTimeout(decorateAllCards, 300);
    setTimeout(decorateAllCards, 1000);
    setTimeout(decorateAllCards, 2500);
    setInterval(decorateAllCards, 3000);

    // Watch for DOM changes to inject tab and decorate cards globally
    new MutationObserver((mutations) => {
        injectRatingsTab();
        let hasCard = false;
        for (const m of mutations) {
            if (m.addedNodes && m.addedNodes.length > 0) {
                for (const n of m.addedNodes) {
                    if (n.nodeType !== 1) continue;
                    if (n.classList && (n.classList.contains('card') || n.querySelector && n.querySelector('.card[data-id]'))) {
                        hasCard = true;
                        break;
                    }
                }
            }
            if (hasCard) break;
        }
        if (hasCard) scheduleGlobalDecorate();
    }).observe(document.body, {
        subtree: true,
        childList: true
    });
})();