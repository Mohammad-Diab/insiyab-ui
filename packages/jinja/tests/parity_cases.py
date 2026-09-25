"""The Jinja side of the parity test: each case builds, with the macros, the example of
the same name in test/fixtures/parity/ (copied from the docs). Every template gets
`{% import "insiyab/ui.html" as ins %}` in front of it."""

CASES = {}

CASES['alerts'] = """
{{ ins.alert('نُشر المنتج، وهو ظاهر الآن في المتجر.', tone='ok', icon=ins.icon('i-check')) }}
{{ ins.alert('صيانة مجدولة ليلة الجمعة من الثانية إلى الرابعة فجرًا.', tone='info', icon=ins.icon('i-info')) }}
{{ ins.alert('بقيت ثلاث قطع فقط من «حقيبة ظهر جلدية».', tone='warn', icon=ins.icon('i-warn')) }}
{{ ins.alert('فشل الدفع: رفض المصرف البطاقة. جرّب بطاقة أخرى.', tone='bad', icon=ins.icon('i-x')) }}
"""

CASES['alert-dismiss'] = """
{{ ins.alert('جديد: يمكنك الآن جدولة نشر المنتجات مسبقًا.', tone='info', icon=ins.icon('i-info'), dismissible=true, close_label='إغلاق التنبيه', id='notice') }}
"""

CASES['pills'] = """
{{ ins.pill('تمّ التسليم', tone='ok') }}{{ ins.pill('بانتظار الدفع', tone='warn') }}{{ ins.pill('ملغى', tone='bad') }}
{{ ins.pill('قيد التجهيز', tone='info') }}{{ ins.pill('مميّز', tone='brand') }}{{ ins.pill('مسودّة') }}
"""

CASES['badges'] = """
{{ ins.badge('12') }}{{ ins.badge('3', tone='ok') }}{{ ins.badge('7', tone='warn') }}{{ ins.badge('99+', tone='bad') }}{{ ins.badge('0', tone='mute') }}
{% call ins.button(variant='secondary') %}الرسائل {{ ins.badge('4', tone='bad') }}{% endcall %}
"""

CASES['avatars'] = """
{{ ins.avatar('ع', size='sm') }}{{ ins.avatar('ل ح') }}{{ ins.avatar('س ي', size='lg') }}
"""

CASES['money'] = """
{{ ins.money('1,249.00', 'ر.س') }}{{ ins.money('980', 'د.إ') }}{{ ins.money('3,500', 'ج.م') }}{{ ins.money('120.50', 'USD') }}
{{ ins.money('+450.00', 'ر.س', sign='pos') }}{{ ins.money('-1,500.00', 'ر.س', sign='neg') }}
"""

CASES['buttons'] = """
{% call ins.button(variant='primary') %}{{ ins.icon('i-check') }}حفظ التغييرات{% endcall %}
{{ ins.button('تأكيد الطلب', variant='success') }}{{ ins.button('حذف المنتج', variant='danger') }}
{{ ins.button('إيقاف مؤقّت', variant='warning') }}{{ ins.button('عرض التفاصيل', variant='info') }}
"""

CASES['buttons-ghost'] = """
{{ ins.button('معاينة', variant='secondary') }}
{% call ins.button(variant='ghost') %}{{ ins.icon('i-download') }}تصدير{% endcall %}
{{ ins.button('قبول', variant='ghost-success') }}{{ ins.button('رفض', variant='ghost-danger') }}{{ ins.button('تأجيل', variant='ghost-warning') }}
{{ ins.button('مساعدة', variant='ghost-info') }}{{ ins.button('إلغاء', variant='bare') }}
"""

CASES['buttons-icon'] = """
{% call ins.button(variant='secondary', icon_only=true, aria_label='تعديل', tip=true) %}{{ ins.icon('i-edit') }}{% endcall %}
{% call ins.button(variant='secondary', icon_only=true, aria_label='مشاركة', tip=true) %}{{ ins.icon('i-share') }}{% endcall %}
{% call ins.button(variant='ghost-danger', icon_only=true, aria_label='حذف', tip=true) %}{{ ins.icon('i-trash') }}{% endcall %}
{% call ins.button(variant='secondary', size='sm', icon_only=true, aria_label='المزيد', tip=true) %}{{ ins.icon('i-dots') }}{% endcall %}
"""

CASES['buttons-states'] = """
{{ ins.button('معطّل', variant='primary', disabled=true) }}{{ ins.button('معطّل', variant='secondary', disabled=true) }}
{{ ins.button('مع ارتفاع · --lift', variant='primary', lift=true) }}{{ ins.button('رابط بشكل زرّ', variant='secondary', href='#states') }}
"""

CASES['toggle-group'] = """
{% call ins.button_group(label='تنسيق النصّ') %}
{% call ins.toggle_button(variant='secondary', icon_only=true, pressed=true, aria_label='عريض', tip=true) %}<b>B</b>{% endcall %}
{% call ins.toggle_button(variant='secondary', icon_only=true, aria_label='تسطير', tip=true) %}<u>U</u>{% endcall %}
{% call ins.toggle_button(variant='secondary', icon_only=true, aria_label='يتوسّطه خطّ', tip=true) %}<s>S</s>{% endcall %}
{% endcall %}
{% call ins.toggle_button(variant='ghost') %}{{ ins.icon('i-star') }}مفضّل{% endcall %}
"""

CASES['split-button'] = """
{% call ins.button_group() %}{{ ins.button('السابق', variant='secondary') }}{{ ins.button('اليوم', variant='secondary') }}{{ ins.button('التالي', variant='secondary') }}{% endcall %}
{% call ins.button_group() %}{{ ins.button('نشر', variant='primary') }}
{% call ins.menu(ins.icon('i-chev-down'), variant='primary', icon_only=true, button_label='خيارات النشر') %}{{ ins.menu_item('جدولة النشر') }}{{ ins.menu_item('حفظ كمسودّة') }}{% endcall %}
{% endcall %}
"""

CASES['close'] = """
{{ ins.close_button() }}{{ ins.close_button(size='sm') }}{{ ins.close_button(bare=true) }}
"""

CASES['checks'] = """
{{ ins.check('الشحن المجاني', checked=true) }}{{ ins.check('تغليف هديّة') }}{{ ins.check('الدفع عند الاستلام (غير متاح)', disabled=true) }}
"""

CASES['indeterminate'] = """
{{ ins.check('كلّ الإشعارات', id='c-all', indeterminate=true) }}
{% call ins.stack(gap=2, class='ins-ps-6', id='c-items') %}
{{ ins.check('طلب جديد', checked=true) }}{{ ins.check('تعليق على منتج') }}{{ ins.check('تقرير أسبوعي') }}
{% endcall %}
"""

CASES['radios'] = """
{{ ins.radio('توصيل عادي', name='ship', checked=true) }}{{ ins.radio('توصيل سريع', name='ship') }}{{ ins.radio('استلام من الفرع', name='ship') }}
"""

CASES['switches'] = """
{{ ins.switch('إشعارات البريد', checked=true) }}
{{ ins.switch('وضع الإجازة', desc='يخفي المتجر من نتائج البحث ويوقف الطلبات الجديدة') }}
"""

CASES['switch-card'] = """
{{ ins.switch('نشر المنتج فورًا', desc='يظهر في المتجر بعد الحفظ مباشرة', card=true, checked=true) }}
"""

CASES['switch-grid'] = """
{% call ins.switch_grid() %}
{{ ins.switch('تأكيد الطلب بالبريد', desc='يُرسل فور الدفع', checked=true) }}
{{ ins.switch('تنبيه نفاد المخزون', desc='حين تقلّ الكمّية عن خمس', checked=true) }}
{{ ins.switch('التقرير اليومي', desc='كلّ صباح الساعة الثامنة') }}
{% endcall %}
"""

CASES['tiles'] = """
{% call ins.tile_grid(label='الخطّة') %}
{{ ins.tile('البداية', desc='حتى ٥٠ منتجًا · مجانًا', name='plan', value='starter') }}
{{ ins.tile('النموّ', desc='منتجات بلا حدّ · ٤٩ شهريًا', name='plan', value='growth', checked=true) }}
{{ ins.tile('الاحتراف', desc='عدّة متاجر وفريق · ١٤٩ شهريًا', name='plan', value='pro') }}
{% endcall %}
"""

CASES['seg'] = """
{{ ins.seg([('day', 'اليوم'), ('week', 'الأسبوع'), ('month', 'الشهر'), ('year', 'السنة')], selected='day', label='الفترة') }}
"""

CASES['color'] = """
{% call ins.field('لون الهوية', id='cp-brand', hint='جرّب لونًا فاتحًا وانظر التباين في أسفل المنتقي.') %}
{{ ins.color(id='cp-brand', name='brand', value='#9b2c5e') }}
{% endcall %}
"""

CASES['dialog'] = """
{{ ins.button('صغير', variant='secondary', dialog='#dlg-sm') }}{{ ins.button('كبير', variant='secondary', dialog='#dlg-lg') }}
{% set foot %}{{ ins.button('إلغاء', variant='bare', dismiss=true) }}{{ ins.button('أرشفة', variant='primary', class='ins-ms-auto', dismiss=true) }}{% endset %}
{% call ins.dialog(id='dlg-sm', size='sm', title='أرشفة الطلب؟', footer=foot) %}<p class="ins-m-0">يبقى الطلب في السجلّ، ويختفي من القائمة الرئيسية.</p>{% endcall %}
{% set title %}تفاصيل الطلب <span class="ins-num">#40218</span>{% endset %}
{% set foot %}{% call ins.button(variant='secondary', dismiss=true) %}{{ ins.icon('i-download') }}الفاتورة{% endcall %}{{ ins.button('تمّ', variant='primary', class='ins-ms-auto', dismiss=true) }}{% endset %}
{% call ins.dialog(id='dlg-lg', size='lg', title=title, footer=foot) %}
{% call ins.table() %}
<thead><tr><th>المنتج</th><th class="ins-num">الكمّية</th><th class="ins-num">السعر</th></tr></thead>
<tbody>
<tr><td>حقيبة ظهر جلدية</td><td class="ins-num">1</td><td class="ins-num">249.00</td></tr>
<tr><td>حافظة بطاقات</td><td class="ins-num">2</td><td class="ins-num">90.00</td></tr>
</tbody>
{% endcall %}
{% endcall %}
"""

CASES['drawer'] = """
{% call ins.button(variant='secondary', dialog='#drawer-filters') %}{{ ins.icon('i-sliders') }}التصفية{% endcall %}
{{ ins.button('تفاصيل الطلب', variant='secondary', dialog='#drawer-order') }}
{% call ins.button(variant='secondary', dialog='#drawer-share') %}{{ ins.icon('i-share') }}مشاركة{% endcall %}
{% set foot %}{{ ins.button('إعادة ضبط', variant='bare', dismiss=true) }}{{ ins.button('عرض النتائج', variant='primary', class='ins-ms-auto', dismiss=true) }}{% endset %}
{% call ins.drawer(id='drawer-filters', title='تصفية المنتجات', footer=foot) %}
{% call ins.field('الفئة', id='dr-cat') %}{{ ins.select(['كلّ الفئات', 'حقائب', 'ساعات', 'إلكترونيات'], id='dr-cat') }}{% endcall %}
{{ ins.check('المتوفّر فقط', checked=true) }}{{ ins.check('عليه خصم') }}
{% endcall %}
{% set title %}الطلب <span class="ins-num">#40218</span>{% endset %}
{% call ins.drawer(id='drawer-order', side='end', title=title) %}
<dl class="ins-prose ins-m-0">
<dt>العميل</dt><dd>ليلى حسن</dd>
<dt>المبلغ</dt><dd>{{ ins.money('249.00', 'ر.س') }}</dd>
<dt>الحالة</dt><dd>{{ ins.pill('قيد التجهيز', tone='info') }}</dd>
</dl>
{% endcall %}
{% call ins.drawer(id='drawer-share', side='bottom', title='مشاركة المنتج') %}
{% call ins.row() %}
{% call ins.button(variant='secondary', dismiss=true) %}{{ ins.icon('i-copy') }}نسخ الرابط{% endcall %}
{% call ins.button(variant='secondary', dismiss=true) %}{{ ins.icon('i-mail') }}بالبريد{% endcall %}
{% endcall %}
{% endcall %}
"""

CASES['menu'] = """
{% set label %}إجراءات{{ ins.icon('i-chev-down') }}{% endset %}
{% call ins.menu(label, start=true) %}
{{ ins.menu_label('المنتج') }}
{% call ins.menu_item() %}{{ ins.icon('i-edit') }}تعديل {{ ins.kbd('E') }}{% endcall %}
{% call ins.menu_item() %}{{ ins.icon('i-copy') }}تكرار{% endcall %}
{{ ins.menu_item('نقل إلى متجر آخر', disabled=true) }}
{{ ins.menu_separator() }}
{% call ins.menu_item(tone='bad') %}{{ ins.icon('i-trash') }}حذف{% endcall %}
{% endcall %}
"""

CASES['menu-checks'] = """
{% set label %}{{ ins.icon('i-table') }}الأعمدة{% endset %}
{% call ins.menu(label, start=true) %}
{{ ins.menu_label('إظهار') }}
{{ ins.menu_checkbox('العميل', checked=true) }}{{ ins.menu_checkbox('المبلغ', checked=true) }}{{ ins.menu_checkbox('طريقة الدفع') }}
{{ ins.menu_separator() }}
{{ ins.menu_label('الترتيب') }}
{% call ins.menu_group() %}{{ ins.menu_radio('الأحدث أوّلًا', checked=true) }}{{ ins.menu_radio('الأعلى مبلغًا') }}{% endcall %}
{% endcall %}
"""

CASES['popover'] = """
{% set label %}{{ ins.avatar('ه م', size='sm') }}هدى منصور{% endset %}
{% call ins.popover(label, variant='ghost', start=true) %}
{% call ins.row(gap=3, class='ins-mb-3') %}{{ ins.avatar('ه م') }}{% call ins.stack(gap=0, tag='span') %}<b>هدى منصور</b><span class="ins-muted ins-text-xs">مديرة المتجر · الرياض</span>{% endcall %}{% endcall %}
<p class="ins-pop-text">تدير الطلبات والفريق منذ آذار ٢٠٢٤. متاحة من الأحد إلى الخميس.</p>
{{ ins.button('إرسال رسالة', variant='secondary', size='sm', full=true, class='ins-mt-3') }}
{% endcall %}
"""

CASES['menu-links'] = """
{% call ins.menu('المساعدة', start=true) %}{{ ins.menu_item('البدء السريع', href='start.html') }}{{ ins.menu_item('أسئلة شائعة', href='faq.html') }}{% endcall %}
"""

CASES['file'] = """
{% call ins.field('المرفقات', id='fl-docs', hint='ملفّات PDF أو صور.') %}
{{ ins.file(id='fl-docs', name='docs', multiple=true, accept='.pdf,image/*', max_size='5MB', max_count=5) }}
{% endcall %}
"""

CASES['file-compact'] = """
{% call ins.field('السيرة الذاتية', id='fl-cv') %}{{ ins.file(id='fl-cv', compact=true, name='cv', accept='.pdf') }}{% endcall %}
"""

CASES['date-hijri'] = """
{% call ins.field('تاريخ الإصدار', id='hj-date') %}{{ ins.date(calendar='hijri', id='hj-date', name='issued', value='2026-09-24') }}{% endcall %}
"""

CASES['addon'] = """
{% call ins.field('السعر', id='g-price') %}{% call ins.input_group() %}{{ ins.input(id='g-price', class='ins-num', inputmode='decimal', value='249.00') }}{{ ins.addon('ر.س') }}{% endcall %}{% endcall %}
{% call ins.field('الوزن', id='g-weight') %}{% call ins.input_group() %}{{ ins.input(id='g-weight', class='ins-num', inputmode='decimal', value='1.2') }}{{ ins.addon('كغ') }}{% endcall %}{% endcall %}
"""

CASES['password'] = """
{% call ins.field('كلمة المرور', id='g-pass') %}{{ ins.password(id='g-pass', value='correct-horse', autocomplete='new-password', toggle_tip=true) }}{% endcall %}
"""

CASES['stepper'] = """
{% call ins.field('الكمّية', id='g-qty') %}{{ ins.number(style='max-inline-size:11rem', id='g-qty', min=1, max=10, value=2) }}{% endcall %}
"""

CASES['group-error'] = """
{% call ins.field('رمز الخصم', id='g-code', error='انتهت صلاحية هذا الرمز.') %}
{% call ins.input_group() %}{{ ins.input(id='g-code', value='SUMMER24', invalid=true, dir='ltr') }}{{ ins.button('تطبيق', variant='secondary') }}{% endcall %}
{% endcall %}
"""

CASES['field-hint'] = """
{% call ins.field('اسم المتجر', id='i-store', hint='يظهر في رأس الفواتير ورسائل البريد.') %}{{ ins.input(id='i-store', placeholder='مثال: متجر الورّاق') }}{% endcall %}
"""

CASES['sizes'] = """
{{ ins.input(size='sm', placeholder='صغير · ins-input--sm', aria_label='صغير') }}
{{ ins.input(placeholder='عادي', aria_label='عادي') }}
{{ ins.input(size='lg', placeholder='كبير · ins-input--lg', aria_label='كبير') }}
{{ ins.select(['قائمة صغيرة'], size='sm', aria_label='قائمة صغيرة') }}
{{ ins.select(['قائمة كبيرة'], size='lg', aria_label='قائمة كبيرة') }}
"""

CASES['field-states'] = """
{% call ins.cols(2) %}
{% call ins.field('معطّل', id='i-dis') %}{{ ins.input(id='i-dis', value='لا يمكن تعديله', disabled=true) }}{% endcall %}
{% call ins.field('للقراءة فقط', id='i-ro') %}{{ ins.input(id='i-ro', class='ins-num', value='STORE-0192', readonly=true) }}{% endcall %}
{% call ins.field('فيه خطأ', id='i-bad', error='أدخل رقمًا صحيحًا.') %}{{ ins.input(id='i-bad', value='abc', invalid=true) }}{% endcall %}
{% call ins.field('صحيح', id='i-ok', success='الاسم متاح.') %}{{ ins.input(id='i-ok', ok=true, value='alwarraq', dir='ltr') }}{% endcall %}
{% endcall %}
"""

CASES['search'] = """
{{ ins.search(icon=ins.icon('i-search', class='ins-search-ico'), placeholder='ابحث عن منتج…', aria_label='بحث') }}
"""

CASES['list'] = """
{% call ins.panel(title='الفريق', bare=true) %}
{% call ins.list() %}
{{ ins.list_item(title='هدى منصور', desc='مديرة المتجر', start=ins.avatar('ه م'), end=ins.pill('متّصلة', tone='ok')) }}
{% call ins.list_item(title='يوسف عادل', desc='خدمة العملاء', start=ins.avatar('ي ع')) %}<span class="ins-list-end ins-muted ins-text-sm">آخر ظهور <span class="ins-num">14:02</span></span>{% endcall %}
{{ ins.list_item(title='رنا فهد', desc='تصوير المنتجات', start=ins.avatar('ر ف'), end=ins.button('رسالة', variant='secondary', size='sm')) }}
{% endcall %}
{% endcall %}
"""

CASES['list-links'] = """
{% macro chip(name) %}<span class="ins-panel-ico">{{ ins.icon(name) }}</span>{% endmacro %}
{% call ins.panel(bare=true) %}
{% call ins.list(tag='nav', aria_label='الإعدادات') %}
{{ ins.list_item(title='عام', desc='الاسم واللغة والعملة', href='#links', current=true, start=chip('i-cog')) }}
{{ ins.list_item(title='الدفع', desc='البطاقات والمحافظ والتحويل', href='#links', start=chip('i-wallet'), end=ins.badge('1', tone='warn')) }}
{{ ins.list_item(title='الأمان', desc='كلمة المرور والتحقّق بخطوتين', href='#links', start=chip('i-lock')) }}
{% endcall %}
{% endcall %}
"""

CASES['spinners'] = """
{{ ins.spinner(label='جارٍ التحميل') }}{{ ins.spinner(label='جارٍ التحميل', size='sm') }}{{ ins.button('جارٍ الحفظ…', variant='primary', loading=true) }}
"""

CASES['loading'] = """
{% call ins.panel(bare=true) %}{{ ins.loading(label='جارٍ تحميل الطلبات') }}{% endcall %}
"""

CASES['empty'] = """
{% call ins.panel(bare=true) %}
{% call ins.empty('لا منتجات بعد', hint='أضف أوّل منتج ليظهر في متجرك. يمكنك استيراد قائمة كاملة من ملفّ أيضًا.', icon=ins.icon('i-box', style='font-size:2rem')) %}
{% call ins.row(justify='center', class='ins-mt-4') %}
{% call ins.button(variant='primary') %}{{ ins.icon('i-plus') }}منتج جديد{% endcall %}
{% call ins.button(variant='secondary') %}{{ ins.icon('i-upload') }}استيراد{% endcall %}
{% endcall %}
{% endcall %}
{% endcall %}
"""

CASES['navbar'] = """
{% set brand %}<span class="ins-shell-mark" style="--ins-mark:30px">و</span>الورّاق{% endset %}
{% set end %}{{ ins.button('دخول', variant='bare') }}{{ ins.button('إنشاء حساب', variant='primary', size='sm') }}{% endset %}
{% call ins.navbar(brand=brand, brand_href='#basic', label='الموقع', end=end, static=true) %}
{{ ins.navbar_link('الرئيسية', '#basic', current=true) }}{{ ins.navbar_link('الكتب', '#basic') }}{{ ins.navbar_link('المؤلّفون', '#basic') }}{{ ins.navbar_link('المدوّنة', '#basic') }}
{% endcall %}
"""

CASES['otp'] = """
{% call ins.field('أربعة أرقام', id='otp-four') %}{{ ins.otp(4, id='otp-four') }}{% endcall %}
{% call ins.field('رمز بحروف', id='otp-alnum') %}{{ ins.otp(6, alnum=true, id='otp-alnum') }}{% endcall %}
<form class="ins-field" id="otp-auto-form"><label class="ins-label" for="otp-auto">يُرسل وحده</label>{{ ins.otp(4, auto_submit=true, id='otp-auto', name='code') }}</form>
"""

CASES['page-head'] = """
{% set crumbs %}{{ ins.breadcrumb([('المتجر', '#head'), ('المنتجات', '#head'), 'حقيبة ظهر جلدية']) }}{% endset %}
{% set sub %}آخر تعديل قبل ساعتين · <span class="ins-num">14</span> قطعة في المخزون{% endset %}
{% set actions %}{{ ins.button('معاينة', variant='secondary') }}{{ ins.button('حفظ التغييرات', variant='primary') }}{% endset %}
{{ ins.page_head('حقيبة ظهر جلدية', eyebrow='منتج', sub=sub, breadcrumb=crumbs, actions=actions, title_tag='h3', class='ins-mb-0') }}
"""

CASES['breadcrumb'] = """
{{ ins.breadcrumb([('الرئيسية', '#breadcrumb'), ('الإعدادات', '#breadcrumb'), 'الفواتير']) }}
"""

CASES['pagination'] = """
{{ ins.pagination(3, 12, label='صفحات الطلبات') }}
"""

CASES['pagination-first'] = """
{{ ins.pagination(1, 3, label='صفحات المنتجات') }}
"""

CASES['phone'] = """
{% set hint %}يصل إلى الخادم: <code class="ins-num" id="ph-demo-out" dir="ltr">+966501234567</code>{% endset %}
{% call ins.field('رقم الجوال', id='ph-demo', hint=hint) %}{{ ins.phone(id='ph-demo', name='mobile', value='+966501234567') }}{% endcall %}
"""

CASES['combo'] = """
{% call ins.field('المدينة', id='p-city', hint='جرّب «الاسكندرية» بلا همزة ولا شدّة، أو «جده» بالهاء، أو «ابو ظبي».') %}
{{ ins.combo([('ruh', 'الرياض'), ('jed', 'جدّة'), ('dxb', 'دبي'), ('auh', 'أبوظبي'), ('doh', 'الدوحة'), ('kwi', 'الكويت'), ('amm', 'عمّان'), ('bey', 'بيروت'), ('cai', 'القاهرة'), ('alx', 'الإسكندريّة'), ('rba', 'الرباط'), ('tun', 'تونس'), ('mct', 'مسقط')], name='city', empty='لا مدينة بهذا الاسم', id='p-city', placeholder='ابدأ الكتابة…', autocomplete='off') }}
{% endcall %}
"""

CASES['date'] = """
{% call ins.field('تاريخ التسليم', id='p-date') %}{{ ins.date(id='p-date', name='delivery', value='2026-09-30', min='2026-09-23') }}{% endcall %}
"""

CASES['date-range'] = """
{% call ins.field_row() %}
{% call ins.field('من', id='p-from', class='ins-grow') %}{{ ins.date(range_end='p-to', id='p-from', name='from', value='2026-09-01') }}{% endcall %}
{% call ins.field('إلى', id='p-to', class='ins-grow') %}{{ ins.date(range_start='p-from', id='p-to', name='to', value='2026-09-23') }}{% endcall %}
{% endcall %}
"""

CASES['range'] = """
{% set label %}نسبة الخصم · <output id="p-disc-out" class="ins-num">20</output>٪{% endset %}
{% call ins.field(label, id='p-disc') %}{{ ins.range(id='p-disc', min=0, max=50, step=5, value=20) }}{% endcall %}
"""

CASES['progress'] = """
{{ ins.progress(40, size='sm', aria_label='صغير') }}{{ ins.progress(55, aria_label='عادي') }}{{ ins.progress(70, size='lg', aria_label='كبير') }}
"""

CASES['ring'] = """
{{ ins.ring(72, 'المهامّ المنجزة') }}{{ ins.ring(45, 'هدف المبيعات', size='lg') }}
"""

CASES['toc'] = """
<div class="spy-demo">
<div class="spy-demo-box" id="spy-box">
<section id="spy-a"><h3>الطلب</h3><p>يصل الطلب من المتجر أو من التطبيق، ويُسجَّل برقمه وتاريخه ومبلغه.</p></section>
<section id="spy-b"><h3>المراجعة</h3><p>يراجع الموظّف البيانات، ويطلب ما نقص منها قبل أن يمضي الطلب.</p></section>
<section id="spy-c"><h3>الدفع</h3><p>يُحصَّل المبلغ، ويُرسَل الإيصال إلى بريد العميل.</p></section>
<section id="spy-d"><h3>التسليم</h3><p>يُشحن الطلب، ويتابعه العميل حتّى يصل.</p></section>
</div>
{{ ins.toc([{'href': '#spy-a', 'label': 'الطلب'}, {'href': '#spy-b', 'label': 'المراجعة'}, {'href': '#spy-c', 'label': 'الدفع', 'sub': true}, {'href': '#spy-d', 'label': 'التسليم'}], title='المراحل', label='مراحل الطلب') }}
</div>
"""

CASES['topbar'] = """
{% set start %}
{% call ins.island(tag='button', variant='icon', aria_label='القائمة') %}{{ ins.icon('i-menu') }}{% endcall %}
{% call ins.island() %}الثلاثاء <span class="ins-num">23</span> أيلول{% endcall %}
{% endset %}
{% set center %}
{{ ins.topbar_title('الطلبات', sub='٨ بانتظار الشحن') }}
{% call ins.island(tag='button', variant='icon', aria_label='بحث') %}{{ ins.icon('i-search') }}{% endcall %}
{% endset %}
{% set end %}
{% call ins.island(tag='button', variant='icon', aria_label='الإشعارات') %}{{ ins.icon('i-bell') }}{% endcall %}
{% call ins.island(tag='button') %}{{ ins.avatar('ه م', size='sm') }}<span>هدى منصور</span>{% endcall %}
{% endset %}
{{ ins.topbar(start=start, center=center, end=end, style='position:relative') }}
"""

CASES['stats'] = """
{% call ins.grid() %}
{{ ins.stat('طلبات اليوم', '31', icon=ins.icon('i-cart')) }}
{{ ins.stat('تمّ تسليمها', '24', icon=ins.icon('i-check'), tone='ok') }}
{{ ins.stat('بانتظار الدفع', '5', icon=ins.icon('i-clock'), tone='warn') }}
{{ ins.stat('دفعات فشلت', '2', icon=ins.icon('i-warn'), tone='bad') }}
{{ ins.stat('عملاء جدد', '9', icon=ins.icon('i-users'), tone='info') }}
{{ ins.stat('شكاوى مفتوحة', '0', icon=ins.icon('i-inbox'), tone='zero') }}
{% endcall %}
"""

CASES['stats-lg'] = """
{% set hint %}أعلى بنسبة <span class="ins-num">12%</span> من آب{% endset %}
{% call ins.grid(wide=true) %}
{{ ins.stat('إيرادات الشهر', '128,960', currency='ر.س', hint=hint, icon=ins.icon('i-wallet'), size='lg') }}
{{ ins.stat('منتجات في المخزون', '412', icon=ins.icon('i-box'), size='sm', tone='mute') }}
{% endcall %}
"""

CASES['steps'] = """
{{ ins.steps(['السلّة', 'العنوان', 'الدفع', 'التأكيد'], current=2) }}
"""

CASES['wizard'] = """
{% call ins.wizard(['المتجر', 'الخطّة', 'التأكيد'], id='store-wizard', finish_label='إنشاء المتجر') %}
{% call ins.wizard_panel('المتجر', class='ins-stack') %}
{% call ins.field('اسم المتجر', id='w-store', required=true, error=true) %}{{ ins.input(id='w-store', required=true) }}{% endcall %}
{% call ins.field('بريد التواصل', id='w-email', required=true, error=true) %}{{ ins.input(id='w-email', type='email', required=true, dir='ltr') }}{% endcall %}
{% endcall %}
{% call ins.wizard_panel('الخطّة', class='ins-stack') %}
{% call ins.tile_grid() %}{{ ins.tile('البداية', desc='مجانًا', name='w-plan', value='starter', required=true) }}{{ ins.tile('النموّ', desc='٤٩ شهريًا', name='w-plan', value='growth') }}{% endcall %}
{% endcall %}
{% call ins.wizard_panel('التأكيد') %}{{ ins.check('أوافق على شروط البيع', required=true) }}{% endcall %}
{% endcall %}
"""

CASES['glass'] = """
{% call ins.cols(3) %}
{% call ins.glass(class='ins-p-5') %}<b>.ins-glass</b><p class="ins-muted ins-text-sm ins-mb-0">السطح الأساسي، بلا حشوة ولا سلوك.</p>{% endcall %}
{% call ins.card(hoverable=true, class='ins-p-5') %}<b>.ins-card</b><p class="ins-muted ins-text-sm ins-mb-0">بطاقة. مع <code>.ins-hoverable</code> ترتفع عند المرور.</p>{% endcall %}
{% call ins.glass(inner=true, class='ins-p-5') %}<b>.ins-glass-inner</b><p class="ins-muted ins-text-sm ins-mb-0">المظهر المسطّح، لسطح داخل سطح.</p>{% endcall %}
{% endcall %}
"""

CASES['panel'] = """
{% set foot %}{{ ins.button('دعوة عضو', variant='primary', size='sm') }}{{ ins.button('نسخ رابط الدعوة', variant='bare', size='sm') }}{% endset %}
{% call ins.panel(title='أعضاء الفريق', icon=ins.icon('i-users'), actions=ins.button('إدارة', variant='bare', size='sm'), footer=foot) %}
<p class="ins-m-0 ins-muted">تسعة أعضاء في ثلاثة أقسام. الدعوات المعلّقة تنتهي بعد سبعة أيّام.</p>
{% endcall %}
"""

CASES['panel-alert'] = """
{% call ins.panel(title='ثلاث دفعات فشلت', icon=ins.icon('i-warn'), alert=true) %}<p class="ins-m-0 ins-muted">البطاقات المرفوضة تحتاج تحديث بياناتها قبل محاولة الخصم التالية.</p>{% endcall %}
"""

CASES['panel-flush'] = """
{% call ins.panel(title='الفواتير', flush=true, note='تُصدر الفاتورة في أوّل كلّ شهر.') %}
{% call ins.list() %}
{{ ins.list_item(title='أيلول ٢٠٢٦', end=ins.pill('مدفوعة', tone='ok')) }}
{{ ins.list_item(title='آب ٢٠٢٦', end=ins.pill('مدفوعة', tone='ok')) }}
{% endcall %}
{% endcall %}
"""

CASES['table'] = """
{% call ins.panel(title='الطلبات الأخيرة', icon=ins.icon('i-cart'), actions=ins.badge('128'), flush=true) %}
{% call ins.table() %}
<thead><tr><th>الطلب</th><th>العميل</th><th>المنتج</th><th class="ins-num">المبلغ</th><th>الحالة</th><th>التاريخ</th><th></th></tr></thead>
<tbody>
<tr><td class="ins-num">#40218</td><td>ليلى حسن</td><td>حقيبة ظهر جلدية</td><td class="ins-num">249.00</td><td>{{ ins.pill('قيد التجهيز', tone='info') }}</td><td><span class="ins-num">2026-09-23</span></td>
<td>{% call ins.menu(ins.icon('i-dots'), size='sm', icon_only=true, button_label='إجراءات') %}{{ ins.menu_item('عرض الطلب') }}{{ ins.menu_item('طباعة الفاتورة') }}{{ ins.menu_separator() }}{{ ins.menu_item('إلغاء الطلب', tone='bad') }}{% endcall %}</td></tr>
<tr aria-selected="true"><td class="ins-num">#40217</td><td>عمر خليل</td><td>ساعة يد كلاسيكية</td><td class="ins-num">1,180.00</td><td>{{ ins.pill('تمّ التسليم', tone='ok') }}</td><td><span class="ins-num">2026-09-22</span></td><td></td></tr>
<tr><td class="ins-num">#40216</td><td>سارة يوسف</td><td>سمّاعات لاسلكية</td><td class="ins-num">399.00</td><td>{{ ins.pill('بانتظار الدفع', tone='warn') }}</td><td><span class="ins-num">2026-09-22</span></td><td></td></tr>
<tr><td class="ins-num">#40215</td><td>كريم نصّار</td><td>مصباح مكتب</td><td class="ins-num">89.50</td><td>{{ ins.pill('مُسترجَع', tone='bad') }}</td><td><span class="ins-num">2026-09-21</span></td><td></td></tr>
</tbody>
{% endcall %}
{% endcall %}
"""

CASES['tabs'] = """
{% set pay %}الدفع {{ ins.badge('1', tone='warn') }}{% endset %}
{% call ins.tablist(label='إعدادات المتجر') %}{{ ins.tab('عام', 't-general') }}{{ ins.tab(pay, 't-payments') }}{{ ins.tab('الشحن', 't-shipping') }}{% endcall %}
{% call ins.tabpanel('t-general') %}{% call ins.panel(body_class='ins-stack ins-gap-2') %}<b>عام</b><span class="ins-muted ins-text-sm">اسم المتجر وعملته ومنطقته الزمنية.</span>{% endcall %}{% endcall %}
{% call ins.tabpanel('t-payments') %}{% call ins.panel(body_class='ins-stack ins-gap-2') %}<b>الدفع</b><span class="ins-muted ins-text-sm">وسيلة واحدة تنتظر التفعيل.</span>{% endcall %}{% endcall %}
{% call ins.tabpanel('t-shipping') %}{% call ins.panel(body_class='ins-stack ins-gap-2') %}<b>الشحن</b><span class="ins-muted ins-text-sm">المناطق والأسعار ومدد التوصيل.</span>{% endcall %}{% endcall %}
"""

CASES['tabs-seg'] = """
{% set strip %}{% call ins.tablist(seg=true) %}{{ ins.tab('اليوم', 's-day', seg=true) }}{{ ins.tab('الأسبوع', 's-week', seg=true) }}{{ ins.tab('الشهر', 's-month', seg=true) }}{% endcall %}{% endset %}
{% call ins.panel(title='المبيعات', actions=strip) %}
{% call ins.tabpanel('s-day') %}{{ ins.money('4,820', 'ر.س') }} <span class="ins-muted">من ٣١ طلبًا</span>{% endcall %}
{% call ins.tabpanel('s-week') %}{{ ins.money('31,405', 'ر.س') }} <span class="ins-muted">من ٢١٢ طلبًا</span>{% endcall %}
{% call ins.tabpanel('s-month') %}{{ ins.money('128,960', 'ر.س') }} <span class="ins-muted">من ٨٩٤ طلبًا</span>{% endcall %}
{% endcall %}
"""

CASES['timeline'] = """
{% set t1 %}<time class="ins-timeline-time" datetime="2026-09-24T08:02">24 سبتمبر، 8:02 ص</time>{% endset %}
{% set t2 %}<time class="ins-timeline-time" datetime="2026-09-24T08:05">24 سبتمبر، 8:05 ص</time>{% endset %}
{% call ins.timeline() %}
{{ ins.timeline_day('الطلب ‎#4821') }}
{{ ins.timeline_item('استُلم الطلب', tone='ok', time=t1) }}
{{ ins.timeline_item('تمّ الدفع', tone='ok', time=t2, body='بطاقة مدى تنتهي بـ 4417 — 1,250.00 ر.س') }}
{{ ins.timeline_item('قيد التجهيز', state='current', time='المستودع الرئيسيّ، الرياض') }}
{{ ins.timeline_item('الشحن', state='pending', time='متوقّع غدًا') }}
{{ ins.timeline_item('التسليم', state='pending') }}
{% endcall %}
"""

CASES['timeline-icons'] = """
{% call ins.timeline() %}
{{ ins.timeline_item('علّقت سارة على الفاتورة', tone='info', icon=ins.icon('i-message', hidden=true), body='«المبلغ يطابق العقد، يمكن اعتمادها.»') }}
{{ ins.timeline_item('تأخّر الاعتماد يومين', tone='warn', icon=ins.icon('i-alert', hidden=true)) }}
{{ ins.timeline_item('اعتُمدت الفاتورة', tone='ok', icon=ins.icon('i-check', hidden=true)) }}
{% endcall %}
"""

CASES['tree'] = """
{% set folder = ins.icon('i-folder', hidden=true) %}{% set page = ins.icon('i-pages', hidden=true) %}
{% set count %}<span class="ins-pill">12</span>{% endset %}
{{ ins.tree([
  {'open': true, 'icon': folder, 'label': 'العقود', 'children': [
    {'href': 'tables.html', 'icon': page, 'label': 'عقد الإيجار 2026.pdf'},
    {'icon': folder, 'label': 'الملاحق', 'children': [{'icon': page, 'label': 'ملحق 1.pdf'}, {'icon': page, 'label': 'ملحق 2.pdf'}]}
  ]},
  {'icon': folder, 'label': 'الفواتير', 'end': count, 'children': [{'icon': page, 'label': 'سبتمبر.xlsx'}]},
  {'icon': page, 'label': 'ملاحظات.txt'}
], 'ملفّات المشروع') }}
"""

CASES['tree-checks'] = """
{{ ins.tree([
  {'open': true, 'value': 'sales', 'label': 'المبيعات', 'children': [
    {'value': 'sales.view', 'checked': true, 'label': 'عرض الطلبات'},
    {'value': 'sales.edit', 'label': 'تعديل الطلبات'},
    {'value': 'sales.refund', 'label': 'الاسترداد'}
  ]},
  {'value': 'hr', 'checked': true, 'label': 'الموارد البشرية', 'children': [
    {'value': 'hr.view', 'label': 'عرض الموظّفين'},
    {'value': 'hr.pay', 'label': 'الرواتب'}
  ]}
], 'صلاحيات الدور', checks=true, name='perm') }}
"""

CASES['type'] = """
{{ ins.display('انسياب') }}
{{ ins.heading('تقرير المبيعات السنوي', level=1, tag='p') }}{{ ins.heading('الربع الثالث', level=2, tag='p') }}
{{ ins.heading('المنطقة الوسطى', level=3, tag='p') }}{{ ins.heading('ملاحظات الفريق', level=4, tag='p') }}
"""

CASES['dividers'] = """
{{ ins.divider() }}{{ ins.divider('أو') }}{{ ins.divider('معلومات الشحن', start=true) }}
{% call ins.row(class='ins-text-sm') %}<span>ملف</span>{{ ins.divider(vertical=true) }}<span>تعديل</span>{{ ins.divider(vertical=true) }}<span>عرض</span>{% endcall %}
"""

CASES['validation'] = """
{% call ins.form(validate=true, class='ins-panel', id='v-form') %}
{{ ins.panel_head('حساب جديد') }}
{% call ins.panel_body(class='ins-stack') %}
{% call ins.field('الاسم', id='v-name', required=true, error=true) %}{{ ins.input(id='v-name', required=true, autocomplete='name') }}{% endcall %}
{% call ins.field('البريد الإلكتروني', id='v-email', required=true, error=true) %}{{ ins.input(id='v-email', type='email', required=true, dir='ltr', autocomplete='email', placeholder='name@example.com') }}{% endcall %}
{% call ins.field('كلمة المرور', id='v-pass', required=true, hint='ثمانية أحرف على الأقل.', error='كلمة المرور قصيرة — ثمانية أحرف على الأقل.') %}{{ ins.password(id='v-pass', required=true, minlength=8, autocomplete='new-password') }}{% endcall %}
{{ ins.check('أوافق على شروط الاستخدام', required=true) }}
{% call ins.form_commit() %}{{ ins.button('إنشاء الحساب', variant='primary') }}{% endcall %}
{% endcall %}
{% endcall %}
"""

CASES['cols'] = """
{% call ins.cols(12) %}<div class="demo-box ins-span-8">span-8</div><div class="demo-box ins-span-4">span-4</div>{% endcall %}
{% call ins.cols(12) %}<div class="demo-box ins-span-3">span-3</div><div class="demo-box ins-span-6">span-6</div><div class="demo-box ins-span-3">span-3</div>{% endcall %}
{% call ins.cols(12) %}<div class="demo-box ins-span-full">span-full</div>{% endcall %}
"""

CASES['accordion'] = """
{% call ins.panel(bare=true) %}{% call ins.accordion() %}
{% call ins.collapse('الخطّة الحالية', open=true, name='billing') %}خطّة النموّ، تتجدّد في الأوّل من تشرين الأوّل.{% endcall %}
{% call ins.collapse('وسيلة الدفع', name='billing') %}بطاقة تنتهي بـ <span class="ins-num">4242</span>.{% endcall %}
{% call ins.collapse('الفواتير السابقة', name='billing') %}اثنتا عشرة فاتورة، كلّها مدفوعة.{% endcall %}
{% endcall %}{% endcall %}
"""

CASES['collapse'] = """
{% call ins.collapse('خيارات متقدّمة', body_class='ins-stack') %}{{ ins.check('إخفاء المنتج من محرّكات البحث') }}{{ ins.check('السماح بالتقييمات', checked=true) }}{% endcall %}
"""

CASES['carousel'] = """
{% call ins.carousel('منتجات', per_view=3) %}
{% call ins.card(class='ins-p-5') %}<b>باقة الأعمال</b><p class="ins-muted">فواتير غير محدودة ومستخدمان.</p>{% endcall %}
{% call ins.card(class='ins-p-5') %}<b>باقة المتاجر</b><p class="ins-muted">ربط المتجر وتقارير المبيعات.</p>{% endcall %}
{% call ins.card(class='ins-p-5') %}<b>باقة الشركات</b><p class="ins-muted">فروع متعدّدة وصلاحيات مفصّلة.</p>{% endcall %}
{% call ins.card(class='ins-p-5') %}<b>باقة المحاسبين</b><p class="ins-muted">عملاء متعدّدون من حساب واحد.</p>{% endcall %}
{% call ins.card(class='ins-p-5') %}<b>باقة الجمعيات</b><p class="ins-muted">سندات القبض والتبرّعات.</p>{% endcall %}
{% endcall %}
"""

CASES['theme-toggles'] = """
{% call ins.theme_toggle(class='ins-btn ins-btn--secondary') %}{{ ins.icon('i-moon') }}تبديل الوضع{% endcall %}
{{ ins.theme_toggle('اتبع النظام', mode='system', class='ins-btn ins-btn--ghost') }}
"""
