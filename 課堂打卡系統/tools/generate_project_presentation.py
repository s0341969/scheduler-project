from __future__ import annotations

from pathlib import Path

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_AUTO_SHAPE_TYPE
from pptx.enum.text import PP_ALIGN
from pptx.util import Cm, Pt


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "課堂打卡系統_專案簡報.pptx"
IMAGES = ROOT / "docs" / "images"

BRAND = RGBColor(10, 92, 89)
BRAND_DARK = RGBColor(8, 63, 61)
ACCENT = RGBColor(211, 140, 50)
INK = RGBColor(29, 39, 51)
MUTED = RGBColor(92, 103, 115)
SURFACE = RGBColor(248, 243, 235)
WHITE = RGBColor(255, 255, 255)
SUCCESS = RGBColor(31, 122, 69)
WARN = RGBColor(181, 72, 54)


def set_background(slide, color: RGBColor) -> None:
    fill = slide.background.fill
    fill.solid()
    fill.fore_color.rgb = color


def add_title(slide, title: str, subtitle: str | None = None) -> None:
    title_box = slide.shapes.add_textbox(Cm(1.4), Cm(0.9), Cm(20.8), Cm(2.1))
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    run = p.add_run()
    run.text = title
    run.font.name = "Microsoft JhengHei"
    run.font.size = Pt(24)
    run.font.bold = True
    run.font.color.rgb = INK

    if subtitle:
        sub_box = slide.shapes.add_textbox(Cm(1.4), Cm(2.2), Cm(20.5), Cm(1.0))
        tf = sub_box.text_frame
        p = tf.paragraphs[0]
        run = p.add_run()
        run.text = subtitle
        run.font.name = "Microsoft JhengHei"
        run.font.size = Pt(10.5)
        run.font.color.rgb = MUTED

    line = slide.shapes.add_shape(
        MSO_AUTO_SHAPE_TYPE.RECTANGLE, Cm(1.4), Cm(3.0), Cm(3.6), Cm(0.18)
    )
    line.fill.solid()
    line.fill.fore_color.rgb = ACCENT
    line.line.fill.background()


def add_bullets(
    slide,
    items: list[str],
    left: float,
    top: float,
    width: float,
    height: float,
    font_size: float = 18,
) -> None:
    box = slide.shapes.add_textbox(Cm(left), Cm(top), Cm(width), Cm(height))
    tf = box.text_frame
    tf.word_wrap = True
    tf.clear()

    for index, item in enumerate(items):
        paragraph = tf.paragraphs[0] if index == 0 else tf.add_paragraph()
        paragraph.text = item
        paragraph.level = 0
        paragraph.space_after = Pt(8)
        paragraph.bullet = True
        paragraph.font.name = "Microsoft JhengHei"
        paragraph.font.size = Pt(font_size)
        paragraph.font.color.rgb = INK


def add_body_text(
    slide,
    text: str,
    left: float,
    top: float,
    width: float,
    height: float,
    font_size: float = 18,
    color: RGBColor = INK,
) -> None:
    box = slide.shapes.add_textbox(Cm(left), Cm(top), Cm(width), Cm(height))
    tf = box.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = text
    p.font.name = "Microsoft JhengHei"
    p.font.size = Pt(font_size)
    p.font.color.rgb = color


def add_panel(slide, left: float, top: float, width: float, height: float, title: str, body: list[str]) -> None:
    shape = slide.shapes.add_shape(
        MSO_AUTO_SHAPE_TYPE.ROUNDED_RECTANGLE,
        Cm(left),
        Cm(top),
        Cm(width),
        Cm(height),
    )
    shape.fill.solid()
    shape.fill.fore_color.rgb = WHITE
    shape.line.color.rgb = RGBColor(223, 214, 201)

    title_box = slide.shapes.add_textbox(Cm(left + 0.4), Cm(top + 0.35), Cm(width - 0.8), Cm(0.8))
    p = title_box.text_frame.paragraphs[0]
    p.text = title
    p.font.name = "Microsoft JhengHei"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = BRAND_DARK

    add_bullets(slide, body, left + 0.35, top + 1.1, width - 0.7, height - 1.3, 13.5)


def add_image(slide, image_name: str, left: float, top: float, width: float, height: float) -> None:
    path = IMAGES / image_name
    if path.exists():
        slide.shapes.add_picture(str(path), Cm(left), Cm(top), width=Cm(width), height=Cm(height))


def add_footer(slide, text: str) -> None:
    footer = slide.shapes.add_textbox(Cm(1.4), Cm(18.3), Cm(23.5), Cm(0.6))
    p = footer.text_frame.paragraphs[0]
    p.text = text
    p.font.name = "Microsoft JhengHei"
    p.font.size = Pt(9)
    p.font.color.rgb = MUTED
    p.alignment = PP_ALIGN.RIGHT


def build_presentation() -> Presentation:
    prs = Presentation()
    prs.slide_width = Cm(25.4)
    prs.slide_height = Cm(19.05)

    blank = prs.slide_layouts[6]

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    banner = slide.shapes.add_shape(MSO_AUTO_SHAPE_TYPE.ROUNDED_RECTANGLE, Cm(1.3), Cm(1.2), Cm(9.6), Cm(1.0))
    banner.fill.solid()
    banner.fill.fore_color.rgb = BRAND
    banner.line.fill.background()
    p = banner.text_frame.paragraphs[0]
    p.text = "Classroom Attendance System"
    p.font.name = "Microsoft JhengHei"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = WHITE
    p.alignment = PP_ALIGN.CENTER
    add_body_text(slide, "課堂打卡系統\n專題簡報", 1.4, 3.0, 9.2, 3.2, 28, INK)
    add_body_text(slide, "從構想到規劃、實作與使用說明", 1.5, 6.5, 10.5, 1.0, 14, MUTED)
    add_image(slide, "home.png", 13.2, 2.0, 10.1, 7.2)
    add_footer(slide, "版本整理日期：2026-05-14")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "專案構想", "為什麼要做這套課堂打卡系統")
    add_panel(slide, 1.4, 4.0, 7.2, 5.6, "痛點", [
        "傳統紙本簽到容易代簽、統計慢、資料難保存",
        "老師臨時開課或調課時，出席管理缺乏彈性",
        "課後整理出席名單與匯出報表耗時",
    ])
    add_panel(slide, 9.2, 4.0, 7.2, 5.6, "目標", [
        "讓教室現場可立即開放打卡",
        "降低代打卡與重複打卡風險",
        "提供後台維護、QR 打卡與 Excel 匯出",
    ])
    add_panel(slide, 17.0, 4.0, 7.0, 5.6, "核心原則", [
        "單機即可執行，不依賴外部資料庫",
        "繁體中文介面，操作直覺",
        "可持續擴充到校務登入與多角色",
    ])
    add_footer(slide, "定位：教室現場可落地的出席管理工具")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "需求規劃", "從功能面拆解系統必備能力")
    add_bullets(slide, [
        "課堂總覽：可快速看到今日課堂、狀態與出席數",
        "學生本人打卡：學生登入後才能提交，不允許手改學號姓名",
        "動態 QR Code：課堂投影掃碼進入打卡頁，Token 具時效性",
        "防重複與可疑標記：重複打卡阻擋，異常網路與裝置行為標示",
        "管理後台：課程維護、課堂維護、打卡開關與報表匯出",
        "班級分層：首頁依一年級至三年級、101-310 班顯示課堂",
    ], 1.6, 4.0, 22.0, 10.5, 17)
    add_footer(slide, "功能規劃先從單機版做穩，再往校務整合延伸")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "系統架構", "技術選型與資料流")
    add_panel(slide, 1.4, 4.0, 5.4, 6.0, "前端 / 介面", [
        "ASP.NET Core MVC",
        "Razor Views",
        "繁體中文教務風格介面",
    ])
    add_panel(slide, 7.2, 4.0, 5.4, 6.0, "業務邏輯", [
        "AttendanceQueryService",
        "Admin / Student 驗證服務",
        "QR Token 驗證與規則判斷",
    ])
    add_panel(slide, 13.0, 4.0, 5.4, 6.0, "資料層", [
        "JSON 檔持久化",
        "App_Data/attendance-data.json",
        "SemaphoreSlim 保護並發寫入",
    ])
    add_panel(slide, 18.8, 4.0, 5.0, 6.0, "外部能力", [
        "ClosedXML 匯出 Excel",
        "QRCoder 產生 QR",
        "Cookie Authentication",
    ])
    add_footer(slide, "選型重點：部署簡單、維護成本低、功能完整")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "實作重點", "從規劃轉成可運作系統")
    add_bullets(slide, [
        "建立課程、課堂、出席紀錄三個核心資料模型",
        "首頁儀表板彙整課堂狀態與近期打卡資訊",
        "加入管理者後台，支援新增 / 編輯 / 刪除課程與課堂",
        "加入學生登入、動態 QR Token、班級 SSID 網段驗證",
        "將風險行為寫入出席紀錄，供後台複核與 Excel 匯出",
        "新增班級欄位與首頁年級班級分組，讓班級課堂一目了然",
    ], 1.6, 4.0, 12.0, 10.6, 16)
    add_image(slide, "admin.png", 14.5, 4.1, 9.1, 6.5)
    add_footer(slide, "實作策略：功能先可用，再逐步補強安全與管理能力")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "首頁總覽", "依年級與班級查看課堂狀態")
    add_image(slide, "home.png", 1.4, 3.7, 14.3, 10.6)
    add_bullets(slide, [
        "首頁顯示開放打卡課堂數、累積打卡筆數與更新時間",
        "固定分為一年級 101-110、二年級 201-210、三年級 301-310",
        "每張課堂卡片提供 QR 打卡板、學生登入與管理切換打卡",
    ], 16.2, 4.2, 7.3, 8.0, 15)
    add_footer(slide, "總覽頁是教務端與現場老師的主要入口")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "學生打卡流程", "登入後掃描 QR Code 完成本人打卡")
    add_panel(slide, 1.4, 4.0, 7.0, 7.4, "流程", [
        "1. 學生先進入 /Student/Login",
        "2. 輸入學號與密碼登入",
        "3. 連上班級 SSID 對應網段",
        "4. 掃描課堂 QR Code 進入打卡頁",
        "5. 系統以登入身份送出打卡",
    ])
    add_image(slide, "login.png", 9.2, 4.2, 6.8, 4.8)
    add_image(slide, "checkin.png", 16.4, 4.2, 7.0, 4.8)
    add_body_text(slide, "示範帳號：S1123001 / Student123! 等三組", 9.3, 9.6, 13.5, 0.8, 12.5, MUTED)
    add_footer(slide, "學生端設計重點：流程短、本人性更高、避免手動造假")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "QR 與安全機制", "降低代打卡與異常打卡風險")
    add_panel(slide, 1.4, 4.0, 7.0, 7.0, "安全設計", [
        "動態 QR Code 預設 5 分鐘失效",
        "同一課堂同一學號不可重複打卡",
        "來源 IP 必須符合班級 SSID 網段規則",
        "記錄 User-Agent、裝置指紋、來源 IP",
        "可疑紀錄在後台集中檢視",
    ])
    add_image(slide, "qr-board.png", 9.2, 4.1, 6.8, 6.0)
    add_body_text(slide, "可疑條件示例：不在允許網段、同裝置短時間被不同學號使用。", 16.5, 4.5, 7.0, 2.0, 14, INK)
    add_body_text(slide, "這不是生物辨識，但已兼顧成本、可維護性與教室現場可行性。", 16.5, 7.0, 7.0, 2.5, 13, MUTED)
    add_footer(slide, "安全策略採漸進式防護，而非一次引入高成本硬體")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "管理者使用說明", "日常操作路徑")
    add_bullets(slide, [
        "登入後台：/Account/Login，使用 admin / ChangeMe123!",
        "課程維護：新增課程時需指定班級，例如 101、205、310",
        "課堂維護：設定主題、起訖時間與是否開放打卡",
        "QR 打卡板：課前投影或列印供學生掃碼",
        "Excel 匯出：課後下載單堂課出席紀錄",
        "可疑紀錄：針對異常來源與裝置行為進行複核",
    ], 1.6, 4.0, 11.8, 11.0, 16)
    add_image(slide, "admin.png", 14.3, 4.0, 9.1, 6.5)
    add_footer(slide, "適合教務、導師或授課老師快速管理課堂出席")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "系統特色整理", "這個專案目前已經做到什麼")
    add_panel(slide, 1.4, 4.0, 5.6, 6.8, "可落地", [
        "單機即可執行",
        "不需外接資料庫",
        "教室現場能直接使用",
    ])
    add_panel(slide, 7.5, 4.0, 5.6, 6.8, "可管理", [
        "後台維護課程與課堂",
        "Excel 匯出",
        "可疑打卡追蹤",
    ])
    add_panel(slide, 13.6, 4.0, 5.6, 6.8, "可擴充", [
        "可接校務單一登入",
        "可接資料庫與多角色",
        "可做統計與儀表板分析",
    ])
    add_panel(slide, 19.7, 4.0, 4.2, 6.8, "目前限制", [
        "仍是單機版",
        "未串校務帳號",
        "未做簽退與請假",
    ])
    add_footer(slide, "目前版本已適合展示概念、流程與實作成果")

    slide = prs.slides.add_slide(blank)
    set_background(slide, SURFACE)
    add_title(slide, "後續發展", "下一階段可以延伸的方向")
    add_bullets(slide, [
        "管理者密碼變更與多帳號權限管理",
        "學生簽退、缺席統計、請假流程",
        "學生帳號與班級 SSID 網段維護後台",
        "校務單一登入整合，取代示範帳號",
        "圖表化出席分析與班級出缺席報表",
        "自動化測試與 CI 驗證，提升維護品質",
    ], 1.7, 4.1, 21.5, 10.5, 17)
    add_footer(slide, "專案已具備可延伸基礎，後續重點在整合與治理")

    slide = prs.slides.add_slide(blank)
    set_background(slide, BRAND_DARK)
    add_body_text(slide, "簡報完畢", 7.4, 5.4, 10.5, 1.4, 30, WHITE)
    add_body_text(slide, "課堂打卡系統", 8.5, 7.3, 8.0, 1.0, 18, RGBColor(231, 219, 199))
    add_body_text(slide, "問題討論 / Demo 展示", 7.6, 9.0, 10.2, 0.9, 16, RGBColor(231, 219, 199))
    add_footer(slide, "G:\\codex_pg\\課堂打卡系統")

    return prs


def main() -> None:
    presentation = build_presentation()
    presentation.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
