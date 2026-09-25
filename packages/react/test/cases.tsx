/* The React side of the parity test: each case builds, with the wrapper's
   components, the example of the same name in test/fixtures/parity/, which is
   copied from the docs. The test renders both, lets the core build both, and
   compares what is in the page. */
import type { CSSProperties, ReactElement } from 'react';
import {
  Accordion, Addon, Alert, Avatar, Badge, Breadcrumb, Button, ButtonGroup, Card, Carousel, Check, CloseButton, Collapse, ColorField,
  Cols, Combo, DateField, Dialog, Display, Divider, Drawer, Empty, Field, FieldRow, FileField, Form, FormCommit, Glass, Grid, Heading,
  Icon, Input, InputGroup, Kbd, List, ListItem, Loading, Menu, MenuCheckbox, MenuGroup, MenuItem, MenuLabel, MenuRadio, MenuSeparator,
  Money, Navbar, NavbarLink, NumberInput, Otp, PageHead, Pagination, Panel, PanelBody, PanelHead, PasswordInput, PhoneField, Pill,
  Popover, Progress, Radio, Range, Ring, Row, Search, Seg, Select, Spinner, Stack, Stat, Steps, Switch, SwitchGrid, Tab, Table, TabList,
  TabPanel, Tabs, ThemeToggle, Tile, TileGrid, Timeline, TimelineDay, TimelineItem, Toc, ToggleButton, Topbar, TopbarTitle, Island, Tree,
  Wizard, WizardPanel
} from '../src/index.js';

const I = (name: string) => (
  <svg className="ico">
    <use href={'#' + name} />
  </svg>
);

export interface Case {
  el: ReactElement;
}

export const cases: Record<string, Case> = {
  alerts: {
    el: (
      <>
        <Alert tone="ok" icon={I('i-check')}>نُشر المنتج، وهو ظاهر الآن في المتجر.</Alert>
        <Alert tone="info" icon={I('i-info')}>صيانة مجدولة ليلة الجمعة من الثانية إلى الرابعة فجرًا.</Alert>
        <Alert tone="warn" icon={I('i-warn')}>بقيت ثلاث قطع فقط من «حقيبة ظهر جلدية».</Alert>
        <Alert tone="bad" icon={I('i-x')}>فشل الدفع: رفض المصرف البطاقة. جرّب بطاقة أخرى.</Alert>
      </>
    )
  },
  'alert-dismiss': {
    el: (
      <Alert tone="info" id="notice" icon={I('i-info')} dismissible closeLabel="إغلاق التنبيه">
        جديد: يمكنك الآن جدولة نشر المنتجات مسبقًا.
      </Alert>
    )
  },
  pills: {
    el: (
      <>
        <Pill tone="ok">تمّ التسليم</Pill>
        <Pill tone="warn">بانتظار الدفع</Pill>
        <Pill tone="bad">ملغى</Pill>
        <Pill tone="info">قيد التجهيز</Pill>
        <Pill tone="brand">مميّز</Pill>
        <Pill>مسودّة</Pill>
      </>
    )
  },
  badges: {
    el: (
      <>
        <Badge>12</Badge>
        <Badge tone="ok">3</Badge>
        <Badge tone="warn">7</Badge>
        <Badge tone="bad">99+</Badge>
        <Badge tone="mute">0</Badge>
        <Button variant="secondary">
          الرسائل <Badge tone="bad">4</Badge>
        </Button>
      </>
    )
  },
  avatars: {
    el: (
      <>
        <Avatar size="sm">ع</Avatar>
        <Avatar>ل ح</Avatar>
        <Avatar size="lg">س ي</Avatar>
      </>
    )
  },
  money: {
    el: (
      <>
        <Money value="1,249.00" currency="ر.س" />
        <Money value="980" currency="د.إ" />
        <Money value="3,500" currency="ج.م" />
        <Money value="120.50" currency="USD" />
        <Money value="+450.00" currency="ر.س" sign="pos" />
        <Money value="-1,500.00" currency="ر.س" sign="neg" />
      </>
    )
  },
  buttons: {
    el: (
      <>
        <Button variant="primary">{I('i-check')}حفظ التغييرات</Button>
        <Button variant="success">تأكيد الطلب</Button>
        <Button variant="danger">حذف المنتج</Button>
        <Button variant="warning">إيقاف مؤقّت</Button>
        <Button variant="info">عرض التفاصيل</Button>
      </>
    )
  },
  'buttons-ghost': {
    el: (
      <>
        <Button variant="secondary">معاينة</Button>
        <Button variant="ghost">{I('i-download')}تصدير</Button>
        <Button variant="ghost-success">قبول</Button>
        <Button variant="ghost-danger">رفض</Button>
        <Button variant="ghost-warning">تأجيل</Button>
        <Button variant="ghost-info">مساعدة</Button>
        <Button variant="bare">إلغاء</Button>
      </>
    )
  },
  'buttons-icon': {
    el: (
      <>
        <Button variant="secondary" iconOnly aria-label="تعديل" tip>{I('i-edit')}</Button>
        <Button variant="secondary" iconOnly aria-label="مشاركة" tip>{I('i-share')}</Button>
        <Button variant="ghost-danger" iconOnly aria-label="حذف" tip>{I('i-trash')}</Button>
        <Button variant="secondary" size="sm" iconOnly aria-label="المزيد" tip>{I('i-dots')}</Button>
      </>
    )
  },
  'buttons-states': {
    el: (
      <>
        <Button variant="primary" disabled>معطّل</Button>
        <Button variant="secondary" disabled>معطّل</Button>
        <Button variant="primary" lift>مع ارتفاع · --lift</Button>
        <Button variant="secondary" href="#states">رابط بشكل زرّ</Button>
      </>
    )
  },
  'toggle-group': {
    el: (
      <>
        <ButtonGroup label="تنسيق النصّ">
          <ToggleButton variant="secondary" iconOnly defaultPressed aria-label="عريض" tip><b>B</b></ToggleButton>
          <ToggleButton variant="secondary" iconOnly aria-label="تسطير" tip><u>U</u></ToggleButton>
          <ToggleButton variant="secondary" iconOnly aria-label="يتوسّطه خطّ" tip><s>S</s></ToggleButton>
        </ButtonGroup>
        <ToggleButton variant="ghost">{I('i-star')}مفضّل</ToggleButton>
      </>
    )
  },
  'split-button': {
    el: (
      <>
        <ButtonGroup>
          <Button variant="secondary">السابق</Button>
          <Button variant="secondary">اليوم</Button>
          <Button variant="secondary">التالي</Button>
        </ButtonGroup>
        <ButtonGroup>
          <Button variant="primary">نشر</Button>
          <Menu variant="primary" iconOnly buttonLabel="خيارات النشر" label={I('i-chev-down')}>
            <MenuItem>جدولة النشر</MenuItem>
            <MenuItem>حفظ كمسودّة</MenuItem>
          </Menu>
        </ButtonGroup>
      </>
    )
  },
  close: {
    el: (
      <>
        <CloseButton />
        <CloseButton size="sm" />
        <CloseButton bare />
      </>
    )
  },
  checks: {
    el: (
      <>
        <Check defaultChecked label="الشحن المجاني" />
        <Check label="تغليف هديّة" />
        <Check disabled label="الدفع عند الاستلام (غير متاح)" />
      </>
    )
  },
  indeterminate: {
    el: (
      <>
        <Check id="c-all" indeterminate label="كلّ الإشعارات" />
        <Stack gap={2} className="ins-ps-6" id="c-items">
          <Check defaultChecked label="طلب جديد" />
          <Check label="تعليق على منتج" />
          <Check label="تقرير أسبوعي" />
        </Stack>
      </>
    )
  },
  radios: {
    el: (
      <>
        <Radio name="ship" defaultChecked label="توصيل عادي" />
        <Radio name="ship" label="توصيل سريع" />
        <Radio name="ship" label="استلام من الفرع" />
      </>
    )
  },
  switches: {
    el: (
      <>
        <Switch defaultChecked label="إشعارات البريد" />
        <Switch label="وضع الإجازة" desc="يخفي المتجر من نتائج البحث ويوقف الطلبات الجديدة" />
      </>
    )
  },
  'switch-card': { el: <Switch card defaultChecked label="نشر المنتج فورًا" desc="يظهر في المتجر بعد الحفظ مباشرة" /> },
  'switch-grid': {
    el: (
      <SwitchGrid>
        <Switch defaultChecked label="تأكيد الطلب بالبريد" desc="يُرسل فور الدفع" />
        <Switch defaultChecked label="تنبيه نفاد المخزون" desc="حين تقلّ الكمّية عن خمس" />
        <Switch label="التقرير اليومي" desc="كلّ صباح الساعة الثامنة" />
      </SwitchGrid>
    )
  },
  tiles: {
    el: (
      <TileGrid label="الخطّة">
        <Tile name="plan" value="starter" title="البداية" desc="حتى ٥٠ منتجًا · مجانًا" />
        <Tile name="plan" value="growth" defaultChecked title="النموّ" desc="منتجات بلا حدّ · ٤٩ شهريًا" />
        <Tile name="plan" value="pro" title="الاحتراف" desc="عدّة متاجر وفريق · ١٤٩ شهريًا" />
      </TileGrid>
    )
  },
  seg: {
    el: (
      <Seg
        label="الفترة"
        defaultValue="day"
        options={[
          { value: 'day', label: 'اليوم' },
          { value: 'week', label: 'الأسبوع' },
          { value: 'month', label: 'الشهر' },
          { value: 'year', label: 'السنة' }
        ]}
      />
    )
  },
  color: {
    el: (
      <Field label="لون الهوية" controlId="cp-brand" hint="جرّب لونًا فاتحًا وانظر التباين في أسفل المنتقي.">
        <ColorField name="brand" defaultValue="#9b2c5e" />
      </Field>
    )
  },
  dialog: {
    el: (
      <>
        <Button variant="secondary" data-ins-dialog="#dlg-sm">صغير</Button>
        <Button variant="secondary" data-ins-dialog="#dlg-lg">كبير</Button>
        <Dialog
          size="sm"
          id="dlg-sm"
          title="أرشفة الطلب؟"
          footer={
            <>
              <Button variant="bare" dismiss>إلغاء</Button>
              <Button variant="primary" className="ins-ms-auto" dismiss>أرشفة</Button>
            </>
          }
        >
          <p className="ins-m-0">يبقى الطلب في السجلّ، ويختفي من القائمة الرئيسية.</p>
        </Dialog>
        <Dialog
          size="lg"
          id="dlg-lg"
          title={
            <>
              تفاصيل الطلب <span className="ins-num">#40218</span>
            </>
          }
          footer={
            <>
              <Button variant="secondary" dismiss>{I('i-download')}الفاتورة</Button>
              <Button variant="primary" className="ins-ms-auto" dismiss>تمّ</Button>
            </>
          }
        >
          <Table>
            <thead>
              <tr><th>المنتج</th><th className="ins-num">الكمّية</th><th className="ins-num">السعر</th></tr>
            </thead>
            <tbody>
              <tr><td>حقيبة ظهر جلدية</td><td className="ins-num">1</td><td className="ins-num">249.00</td></tr>
              <tr><td>حافظة بطاقات</td><td className="ins-num">2</td><td className="ins-num">90.00</td></tr>
            </tbody>
          </Table>
        </Dialog>
      </>
    )
  },
  drawer: {
    el: (
      <>
        <Button variant="secondary" data-ins-dialog="#drawer-filters">{I('i-sliders')}التصفية</Button>
        <Button variant="secondary" data-ins-dialog="#drawer-order">تفاصيل الطلب</Button>
        <Button variant="secondary" data-ins-dialog="#drawer-share">{I('i-share')}مشاركة</Button>
        <Drawer
          id="drawer-filters"
          title="تصفية المنتجات"
          footer={
            <>
              <Button variant="bare" dismiss>إعادة ضبط</Button>
              <Button variant="primary" className="ins-ms-auto" dismiss>عرض النتائج</Button>
            </>
          }
        >
          <Field label="الفئة" controlId="dr-cat">
            <Select><option>كلّ الفئات</option><option>حقائب</option><option>ساعات</option><option>إلكترونيات</option></Select>
          </Field>
          <Check defaultChecked label="المتوفّر فقط" />
          <Check label="عليه خصم" />
        </Drawer>
        <Drawer
          side="end"
          id="drawer-order"
          title={
            <>
              الطلب <span className="ins-num">#40218</span>
            </>
          }
        >
          <dl className="ins-prose ins-m-0">
            <dt>العميل</dt><dd>ليلى حسن</dd>
            <dt>المبلغ</dt><dd><Money value="249.00" currency="ر.س" /></dd>
            <dt>الحالة</dt><dd><Pill tone="info">قيد التجهيز</Pill></dd>
          </dl>
        </Drawer>
        <Drawer side="bottom" id="drawer-share" title="مشاركة المنتج">
          <Row>
            <Button variant="secondary" dismiss>{I('i-copy')}نسخ الرابط</Button>
            <Button variant="secondary" dismiss>{I('i-mail')}بالبريد</Button>
          </Row>
        </Drawer>
      </>
    )
  },
  menu: {
    el: (
      <Menu start label={<>إجراءات{I('i-chev-down')}</>}>
        <MenuLabel>المنتج</MenuLabel>
        <MenuItem>{I('i-edit')}تعديل <Kbd>E</Kbd></MenuItem>
        <MenuItem>{I('i-copy')}تكرار</MenuItem>
        <MenuItem disabled>نقل إلى متجر آخر</MenuItem>
        <MenuSeparator />
        <MenuItem tone="bad">{I('i-trash')}حذف</MenuItem>
      </Menu>
    )
  },
  'menu-checks': {
    el: (
      <Menu start label={<>{I('i-table')}الأعمدة</>}>
        <MenuLabel>إظهار</MenuLabel>
        <MenuCheckbox defaultChecked>العميل</MenuCheckbox>
        <MenuCheckbox defaultChecked>المبلغ</MenuCheckbox>
        <MenuCheckbox>طريقة الدفع</MenuCheckbox>
        <MenuSeparator />
        <MenuLabel>الترتيب</MenuLabel>
        <MenuGroup>
          <MenuRadio defaultChecked>الأحدث أوّلًا</MenuRadio>
          <MenuRadio>الأعلى مبلغًا</MenuRadio>
        </MenuGroup>
      </Menu>
    )
  },
  popover: {
    el: (
      <Popover start variant="ghost" label={<><Avatar size="sm">ه م</Avatar>هدى منصور</>}>
        <Row gap={3} className="ins-mb-3">
          <Avatar>ه م</Avatar>
          <Stack as="span" gap={0}><b>هدى منصور</b><span className="ins-muted ins-text-xs">مديرة المتجر · الرياض</span></Stack>
        </Row>
        <p className="ins-pop-text">تدير الطلبات والفريق منذ آذار ٢٠٢٤. متاحة من الأحد إلى الخميس.</p>
        <Button variant="secondary" size="sm" full className="ins-mt-3">إرسال رسالة</Button>
      </Popover>
    )
  },
  'menu-links': {
    el: (
      <Menu start label="المساعدة">
        <MenuItem href="start.html">البدء السريع</MenuItem>
        <MenuItem href="faq.html">أسئلة شائعة</MenuItem>
      </Menu>
    )
  },
  file: {
    el: (
      <Field label="المرفقات" controlId="fl-docs" hint="ملفّات PDF أو صور.">
        <FileField name="docs" multiple accept=".pdf,image/*" maxSize="5MB" maxCount={5} />
      </Field>
    )
  },
  'file-compact': {
    el: (
      <Field label="السيرة الذاتية" controlId="fl-cv">
        <FileField compact name="cv" accept=".pdf" />
      </Field>
    )
  },
  'date-hijri': {
    el: (
      <Field label="تاريخ الإصدار" controlId="hj-date">
        <DateField calendar="hijri" name="issued" defaultValue="2026-09-24" />
      </Field>
    )
  },
  addon: {
    el: (
      <>
        <Field label="السعر" controlId="g-price">
          <InputGroup>
            <Input className="ins-num" inputMode="decimal" defaultValue="249.00" />
            <Addon>ر.س</Addon>
          </InputGroup>
        </Field>
        <Field label="الوزن" controlId="g-weight">
          <InputGroup>
            <Input className="ins-num" inputMode="decimal" defaultValue="1.2" />
            <Addon>كغ</Addon>
          </InputGroup>
        </Field>
      </>
    )
  },
  password: {
    el: (
      <Field label="كلمة المرور" controlId="g-pass">
        <PasswordInput defaultValue="correct-horse" autoComplete="new-password" toggleTip />
      </Field>
    )
  },
  stepper: {
    el: (
      <Field label="الكمّية" controlId="g-qty">
        <NumberInput style={{ maxInlineSize: '11rem' }} min={1} max={10} defaultValue={2} />
      </Field>
    )
  },
  'group-error': {
    el: (
      <Field label="رمز الخصم" controlId="g-code" error="انتهت صلاحية هذا الرمز.">
        <InputGroup>
          <Input defaultValue="SUMMER24" dir="ltr" />
          <Button variant="secondary">تطبيق</Button>
        </InputGroup>
      </Field>
    )
  },
  'field-hint': {
    el: (
      <Field label="اسم المتجر" controlId="i-store" hint="يظهر في رأس الفواتير ورسائل البريد.">
        <Input placeholder="مثال: متجر الورّاق" />
      </Field>
    )
  },
  sizes: {
    el: (
      <>
        <Input size="sm" placeholder="صغير · ins-input--sm" aria-label="صغير" />
        <Input placeholder="عادي" aria-label="عادي" />
        <Input size="lg" placeholder="كبير · ins-input--lg" aria-label="كبير" />
        <Select size="sm" aria-label="قائمة صغيرة"><option>قائمة صغيرة</option></Select>
        <Select size="lg" aria-label="قائمة كبيرة"><option>قائمة كبيرة</option></Select>
      </>
    )
  },
  'field-states': {
    el: (
      <Cols n={2}>
        <Field label="معطّل" controlId="i-dis">
          <Input defaultValue="لا يمكن تعديله" disabled />
        </Field>
        <Field label="للقراءة فقط" controlId="i-ro">
          <Input className="ins-num" defaultValue="STORE-0192" readOnly />
        </Field>
        <Field label="فيه خطأ" controlId="i-bad" error="أدخل رقمًا صحيحًا.">
          <Input defaultValue="abc" />
        </Field>
        <Field label="صحيح" controlId="i-ok" success="الاسم متاح.">
          <Input ok defaultValue="alwarraq" dir="ltr" />
        </Field>
      </Cols>
    )
  },
  search: { el: <Search icon={I('i-search')} placeholder="ابحث عن منتج…" aria-label="بحث" /> },
  list: {
    el: (
      <Panel title="الفريق" bare>
        <List>
          <ListItem start={<Avatar>ه م</Avatar>} title="هدى منصور" desc="مديرة المتجر" end={<Pill tone="ok">متّصلة</Pill>} />
          <ListItem start={<Avatar>ي ع</Avatar>} title="يوسف عادل" desc="خدمة العملاء">
            <span className="ins-list-end ins-muted ins-text-sm">آخر ظهور <span className="ins-num">14:02</span></span>
          </ListItem>
          <ListItem start={<Avatar>ر ف</Avatar>} title="رنا فهد" desc="تصوير المنتجات" end={<Button variant="secondary" size="sm">رسالة</Button>} />
        </List>
      </Panel>
    )
  },
  'list-links': {
    el: (
      <Panel bare>
        <List as="nav" aria-label="الإعدادات">
          <ListItem href="#links" current start={<span className="ins-panel-ico">{I('i-cog')}</span>} title="عام" desc="الاسم واللغة والعملة" />
          <ListItem href="#links" start={<span className="ins-panel-ico">{I('i-wallet')}</span>} title="الدفع" desc="البطاقات والمحافظ والتحويل" end={<Badge tone="warn">1</Badge>} />
          <ListItem href="#links" start={<span className="ins-panel-ico">{I('i-lock')}</span>} title="الأمان" desc="كلمة المرور والتحقّق بخطوتين" />
        </List>
      </Panel>
    )
  },
  spinners: {
    el: (
      <>
        <Spinner label="جارٍ التحميل" />
        <Spinner size="sm" label="جارٍ التحميل" />
        <Button variant="primary" loading>جارٍ الحفظ…</Button>
      </>
    )
  },
  loading: {
    el: (
      <Panel bare>
        <Loading label="جارٍ تحميل الطلبات" />
      </Panel>
    )
  },
  empty: {
    el: (
      <Panel bare>
        <Empty
          icon={<svg className="ico" style={{ fontSize: '2rem' }}><use href="#i-box" /></svg>}
          title="لا منتجات بعد"
          hint="أضف أوّل منتج ليظهر في متجرك. يمكنك استيراد قائمة كاملة من ملفّ أيضًا."
        >
          <Row justify="center" className="ins-mt-4">
            <Button variant="primary">{I('i-plus')}منتج جديد</Button>
            <Button variant="secondary">{I('i-upload')}استيراد</Button>
          </Row>
        </Empty>
      </Panel>
    )
  },
  navbar: {
    el: (
      <Navbar
        static
        brandHref="#basic"
        brand={<><span className="ins-shell-mark" style={{ '--ins-mark': '30px' } as CSSProperties}>و</span>الورّاق</>}
        label="الموقع"
        end={
          <>
            <Button variant="bare">دخول</Button>
            <Button variant="primary" size="sm">إنشاء حساب</Button>
          </>
        }
      >
        <NavbarLink href="#basic" current>الرئيسية</NavbarLink>
        <NavbarLink href="#basic">الكتب</NavbarLink>
        <NavbarLink href="#basic">المؤلّفون</NavbarLink>
        <NavbarLink href="#basic">المدوّنة</NavbarLink>
      </Navbar>
    )
  },
  otp: {
    el: (
      <>
        <Field label="أربعة أرقام" controlId="otp-four">
          <Otp length={4} />
        </Field>
        <Field label="رمز بحروف" controlId="otp-alnum">
          <Otp length={6} alnum />
        </Field>
        <form className="ins-field" id="otp-auto-form">
          <label className="ins-label" htmlFor="otp-auto">يُرسل وحده</label>
          <Otp id="otp-auto" length={4} autoSubmit name="code" />
        </form>
      </>
    )
  },
  'page-head': {
    el: (
      <PageHead
        className="ins-mb-0"
        breadcrumb={<Breadcrumb items={[{ label: 'المتجر', href: '#head' }, { label: 'المنتجات', href: '#head' }, { label: 'حقيبة ظهر جلدية' }]} />}
        eyebrow="منتج"
        title="حقيبة ظهر جلدية"
        titleAs="h3"
        sub={<>آخر تعديل قبل ساعتين · <span className="ins-num">14</span> قطعة في المخزون</>}
        actions={
          <>
            <Button variant="secondary">معاينة</Button>
            <Button variant="primary">حفظ التغييرات</Button>
          </>
        }
      />
    )
  },
  breadcrumb: {
    el: <Breadcrumb items={[{ label: 'الرئيسية', href: '#breadcrumb' }, { label: 'الإعدادات', href: '#breadcrumb' }, { label: 'الفواتير' }]} />
  },
  pagination: { el: <Pagination label="صفحات الطلبات" page={3} count={12} href={(n) => '?page=' + n} /> },
  'pagination-first': { el: <Pagination label="صفحات المنتجات" page={1} count={3} href={(n) => '?page=' + n} /> },
  phone: {
    el: (
      <Field
        label="رقم الجوال"
        controlId="ph-demo"
        hint={<>يصل إلى الخادم: <code className="ins-num" id="ph-demo-out" dir="ltr">+966501234567</code></>}
      >
        <PhoneField name="mobile" defaultValue="+966501234567" />
      </Field>
    )
  },
  combo: {
    el: (
      <Field label="المدينة" controlId="p-city" hint="جرّب «الاسكندرية» بلا همزة ولا شدّة، أو «جده» بالهاء، أو «ابو ظبي».">
        <Combo
          placeholder="ابدأ الكتابة…"
          name="city"
          emptyText="لا مدينة بهذا الاسم"
          options={[
            { value: 'ruh', label: 'الرياض' }, { value: 'jed', label: 'جدّة' }, { value: 'dxb', label: 'دبي' }, { value: 'auh', label: 'أبوظبي' },
            { value: 'doh', label: 'الدوحة' }, { value: 'kwi', label: 'الكويت' }, { value: 'amm', label: 'عمّان' }, { value: 'bey', label: 'بيروت' },
            { value: 'cai', label: 'القاهرة' }, { value: 'alx', label: 'الإسكندريّة' }, { value: 'rba', label: 'الرباط' }, { value: 'tun', label: 'تونس' },
            { value: 'mct', label: 'مسقط' }
          ]}
        />
      </Field>
    )
  },
  date: {
    el: (
      <Field label="تاريخ التسليم" controlId="p-date">
        <DateField name="delivery" defaultValue="2026-09-30" min="2026-09-23" />
      </Field>
    )
  },
  'date-range': {
    el: (
      <FieldRow>
        <Field className="ins-grow" label="من" controlId="p-from">
          <DateField rangeEnd="p-to" name="from" defaultValue="2026-09-01" />
        </Field>
        <Field className="ins-grow" label="إلى" controlId="p-to">
          <DateField rangeStart="p-from" name="to" defaultValue="2026-09-23" />
        </Field>
      </FieldRow>
    )
  },
  range: {
    el: (
      <Field label={<>نسبة الخصم · <output id="p-disc-out" className="ins-num">20</output>٪</>} controlId="p-disc">
        <Range min={0} max={50} step={5} defaultValue={20} />
      </Field>
    )
  },
  progress: {
    el: (
      <>
        <Progress size="sm" value={40} aria-label="صغير" />
        <Progress value={55} aria-label="عادي" />
        <Progress size="lg" value={70} aria-label="كبير" />
      </>
    )
  },
  ring: {
    el: (
      <>
        <Ring value={72} label="المهامّ المنجزة" />
        <Ring size="lg" value={45} label="هدف المبيعات" />
      </>
    )
  },
  toc: {
    el: (
      <div className="spy-demo">
        <div className="spy-demo-box" id="spy-box">
          <section id="spy-a"><h3>الطلب</h3><p>يصل الطلب من المتجر أو من التطبيق، ويُسجَّل برقمه وتاريخه ومبلغه.</p></section>
          <section id="spy-b"><h3>المراجعة</h3><p>يراجع الموظّف البيانات، ويطلب ما نقص منها قبل أن يمضي الطلب.</p></section>
          <section id="spy-c"><h3>الدفع</h3><p>يُحصَّل المبلغ، ويُرسَل الإيصال إلى بريد العميل.</p></section>
          <section id="spy-d"><h3>التسليم</h3><p>يُشحن الطلب، ويتابعه العميل حتّى يصل.</p></section>
        </div>
        <Toc
          label="مراحل الطلب"
          title="المراحل"
          links={[
            { href: '#spy-a', label: 'الطلب' },
            { href: '#spy-b', label: 'المراجعة' },
            { href: '#spy-c', label: 'الدفع', sub: true },
            { href: '#spy-d', label: 'التسليم' }
          ]}
        />
      </div>
    )
  },
  topbar: {
    el: (
      <Topbar
        style={{ position: 'relative' }}
        start={
          <>
            <Island as="button" variant="icon" aria-label="القائمة">{I('i-menu')}</Island>
            <Island>الثلاثاء <span className="ins-num">23</span> أيلول</Island>
          </>
        }
        center={
          <>
            <TopbarTitle title="الطلبات" sub="٨ بانتظار الشحن" />
            <Island as="button" variant="icon" aria-label="بحث">{I('i-search')}</Island>
          </>
        }
        end={
          <>
            <Island as="button" variant="icon" aria-label="الإشعارات">{I('i-bell')}</Island>
            <Island as="button">
              <Avatar size="sm">ه م</Avatar>
              <span>هدى منصور</span>
            </Island>
          </>
        }
      />
    )
  },
  stats: {
    el: (
      <Grid>
        <Stat icon={I('i-cart')} label="طلبات اليوم" value="31" />
        <Stat tone="ok" icon={I('i-check')} label="تمّ تسليمها" value="24" />
        <Stat tone="warn" icon={I('i-clock')} label="بانتظار الدفع" value="5" />
        <Stat tone="bad" icon={I('i-warn')} label="دفعات فشلت" value="2" />
        <Stat tone="info" icon={I('i-users')} label="عملاء جدد" value="9" />
        <Stat tone="zero" icon={I('i-inbox')} label="شكاوى مفتوحة" value="0" />
      </Grid>
    )
  },
  'stats-lg': {
    el: (
      <Grid wide>
        <Stat size="lg" icon={I('i-wallet')} label="إيرادات الشهر" value="128,960" currency="ر.س" hint={<>أعلى بنسبة <span className="ins-num">12%</span> من آب</>} />
        <Stat size="sm" tone="mute" icon={I('i-box')} label="منتجات في المخزون" value="412" />
      </Grid>
    )
  },
  steps: { el: <Steps items={['السلّة', 'العنوان', 'الدفع', 'التأكيد']} current={2} /> },
  wizard: {
    el: (
      <Wizard id="store-wizard" steps={['المتجر', 'الخطّة', 'التأكيد']} finishLabel="إنشاء المتجر">
        <WizardPanel label="المتجر" className="ins-stack">
          <Field required label="اسم المتجر" controlId="w-store">
            <Input />
          </Field>
          <Field required label="بريد التواصل" controlId="w-email">
            <Input type="email" dir="ltr" />
          </Field>
        </WizardPanel>
        <WizardPanel label="الخطّة" className="ins-stack">
          <TileGrid>
            <Tile name="w-plan" value="starter" required title="البداية" desc="مجانًا" />
            <Tile name="w-plan" value="growth" title="النموّ" desc="٤٩ شهريًا" />
          </TileGrid>
        </WizardPanel>
        <WizardPanel label="التأكيد">
          <Check required label="أوافق على شروط البيع" />
        </WizardPanel>
      </Wizard>
    )
  },
  glass: {
    el: (
      <Cols n={3}>
        <Glass className="ins-p-5">
          <b>.ins-glass</b>
          <p className="ins-muted ins-text-sm ins-mb-0">السطح الأساسي، بلا حشوة ولا سلوك.</p>
        </Glass>
        <Card hoverable className="ins-p-5">
          <b>.ins-card</b>
          <p className="ins-muted ins-text-sm ins-mb-0">بطاقة. مع <code>.ins-hoverable</code> ترتفع عند المرور.</p>
        </Card>
        <Glass inner className="ins-p-5">
          <b>.ins-glass-inner</b>
          <p className="ins-muted ins-text-sm ins-mb-0">المظهر المسطّح، لسطح داخل سطح.</p>
        </Glass>
      </Cols>
    )
  },
  panel: {
    el: (
      <Panel
        icon={I('i-users')}
        title="أعضاء الفريق"
        actions={<Button variant="bare" size="sm">إدارة</Button>}
        footer={
          <>
            <Button variant="primary" size="sm">دعوة عضو</Button>
            <Button variant="bare" size="sm">نسخ رابط الدعوة</Button>
          </>
        }
      >
        <p className="ins-m-0 ins-muted">تسعة أعضاء في ثلاثة أقسام. الدعوات المعلّقة تنتهي بعد سبعة أيّام.</p>
      </Panel>
    )
  },
  'panel-alert': {
    el: (
      <Panel alert icon={I('i-warn')} title="ثلاث دفعات فشلت">
        <p className="ins-m-0 ins-muted">البطاقات المرفوضة تحتاج تحديث بياناتها قبل محاولة الخصم التالية.</p>
      </Panel>
    )
  },
  'panel-flush': {
    el: (
      <Panel title="الفواتير" flush note="تُصدر الفاتورة في أوّل كلّ شهر.">
        <List>
          <ListItem title="أيلول ٢٠٢٦" end={<Pill tone="ok">مدفوعة</Pill>} />
          <ListItem title="آب ٢٠٢٦" end={<Pill tone="ok">مدفوعة</Pill>} />
        </List>
      </Panel>
    )
  },
  table: {
    el: (
      <Panel icon={I('i-cart')} title="الطلبات الأخيرة" actions={<Badge>128</Badge>} flush>
        <Table>
          <thead>
            <tr><th>الطلب</th><th>العميل</th><th>المنتج</th><th className="ins-num">المبلغ</th><th>الحالة</th><th>التاريخ</th><th></th></tr>
          </thead>
          <tbody>
            <tr>
              <td className="ins-num">#40218</td>
              <td>ليلى حسن</td>
              <td>حقيبة ظهر جلدية</td>
              <td className="ins-num">249.00</td>
              <td><Pill tone="info">قيد التجهيز</Pill></td>
              <td><span className="ins-num">2026-09-23</span></td>
              <td>
                <Menu size="sm" iconOnly buttonLabel="إجراءات" label={I('i-dots')}>
                  <MenuItem>عرض الطلب</MenuItem>
                  <MenuItem>طباعة الفاتورة</MenuItem>
                  <MenuSeparator />
                  <MenuItem tone="bad">إلغاء الطلب</MenuItem>
                </Menu>
              </td>
            </tr>
            <tr aria-selected="true">
              <td className="ins-num">#40217</td>
              <td>عمر خليل</td>
              <td>ساعة يد كلاسيكية</td>
              <td className="ins-num">1,180.00</td>
              <td><Pill tone="ok">تمّ التسليم</Pill></td>
              <td><span className="ins-num">2026-09-22</span></td>
              <td></td>
            </tr>
            <tr>
              <td className="ins-num">#40216</td>
              <td>سارة يوسف</td>
              <td>سمّاعات لاسلكية</td>
              <td className="ins-num">399.00</td>
              <td><Pill tone="warn">بانتظار الدفع</Pill></td>
              <td><span className="ins-num">2026-09-22</span></td>
              <td></td>
            </tr>
            <tr>
              <td className="ins-num">#40215</td>
              <td>كريم نصّار</td>
              <td>مصباح مكتب</td>
              <td className="ins-num">89.50</td>
              <td><Pill tone="bad">مُسترجَع</Pill></td>
              <td><span className="ins-num">2026-09-21</span></td>
              <td></td>
            </tr>
          </tbody>
        </Table>
      </Panel>
    )
  },
  tabs: {
    el: (
      <Tabs defaultValue="general">
        <TabList label="إعدادات المتجر">
          <Tab value="general">عام</Tab>
          <Tab value="payments">الدفع <Badge tone="warn">1</Badge></Tab>
          <Tab value="shipping">الشحن</Tab>
        </TabList>
        <TabPanel value="general">
          <Panel bodyClassName="ins-stack ins-gap-2"><b>عام</b><span className="ins-muted ins-text-sm">اسم المتجر وعملته ومنطقته الزمنية.</span></Panel>
        </TabPanel>
        <TabPanel value="payments">
          <Panel bodyClassName="ins-stack ins-gap-2"><b>الدفع</b><span className="ins-muted ins-text-sm">وسيلة واحدة تنتظر التفعيل.</span></Panel>
        </TabPanel>
        <TabPanel value="shipping">
          <Panel bodyClassName="ins-stack ins-gap-2"><b>الشحن</b><span className="ins-muted ins-text-sm">المناطق والأسعار ومدد التوصيل.</span></Panel>
        </TabPanel>
      </Tabs>
    )
  },
  'tabs-seg': {
    el: (
      <Tabs defaultValue="day">
        <Panel
          title="المبيعات"
          actions={
            <TabList seg>
              <Tab value="day">اليوم</Tab>
              <Tab value="week">الأسبوع</Tab>
              <Tab value="month">الشهر</Tab>
            </TabList>
          }
        >
          <TabPanel value="day"><Money value="4,820" currency="ر.س" /> <span className="ins-muted">من ٣١ طلبًا</span></TabPanel>
          <TabPanel value="week"><Money value="31,405" currency="ر.س" /> <span className="ins-muted">من ٢١٢ طلبًا</span></TabPanel>
          <TabPanel value="month"><Money value="128,960" currency="ر.س" /> <span className="ins-muted">من ٨٩٤ طلبًا</span></TabPanel>
        </Panel>
      </Tabs>
    )
  },
  timeline: {
    el: (
      <Timeline>
        <TimelineDay>{'الطلب ‎#4821'}</TimelineDay>
        <TimelineItem tone="ok" title="استُلم الطلب" time={<time dateTime="2026-09-24T08:02">24 سبتمبر، 8:02 ص</time>} />
        <TimelineItem tone="ok" title="تمّ الدفع" time={<time dateTime="2026-09-24T08:05">24 سبتمبر، 8:05 ص</time>} body="بطاقة مدى تنتهي بـ 4417 — 1,250.00 ر.س" />
        <TimelineItem state="current" title="قيد التجهيز" time="المستودع الرئيسيّ، الرياض" />
        <TimelineItem state="pending" title="الشحن" time="متوقّع غدًا" />
        <TimelineItem state="pending" title="التسليم" />
      </Timeline>
    )
  },
  'timeline-icons': {
    el: (
      <Timeline>
        <TimelineItem tone="info" icon={<Icon name="i-message" />} title="علّقت سارة على الفاتورة" body="«المبلغ يطابق العقد، يمكن اعتمادها.»" />
        <TimelineItem tone="warn" icon={<Icon name="i-alert" />} title="تأخّر الاعتماد يومين" />
        <TimelineItem tone="ok" icon={<Icon name="i-check" />} title="اعتُمدت الفاتورة" />
      </Timeline>
    )
  },
  tree: {
    el: (
      <Tree
        label="ملفّات المشروع"
        items={[
          {
            open: true,
            icon: <Icon name="i-folder" />,
            label: 'العقود',
            children: [
              { href: 'tables.html', icon: <Icon name="i-pages" />, label: 'عقد الإيجار 2026.pdf' },
              {
                icon: <Icon name="i-folder" />,
                label: 'الملاحق',
                children: [
                  { icon: <Icon name="i-pages" />, label: 'ملحق 1.pdf' },
                  { icon: <Icon name="i-pages" />, label: 'ملحق 2.pdf' }
                ]
              }
            ]
          },
          {
            icon: <Icon name="i-folder" />,
            label: 'الفواتير',
            end: <span className="ins-pill">12</span>,
            children: [{ icon: <Icon name="i-pages" />, label: 'سبتمبر.xlsx' }]
          },
          { icon: <Icon name="i-pages" />, label: 'ملاحظات.txt' }
        ]}
      />
    )
  },
  'tree-checks': {
    el: (
      <Tree
        checks
        name="perm"
        label="صلاحيات الدور"
        items={[
          {
            open: true,
            value: 'sales',
            label: 'المبيعات',
            children: [
              { value: 'sales.view', checked: true, label: 'عرض الطلبات' },
              { value: 'sales.edit', label: 'تعديل الطلبات' },
              { value: 'sales.refund', label: 'الاسترداد' }
            ]
          },
          {
            value: 'hr',
            checked: true,
            label: 'الموارد البشرية',
            children: [
              { value: 'hr.view', label: 'عرض الموظّفين' },
              { value: 'hr.pay', label: 'الرواتب' }
            ]
          }
        ]}
      />
    )
  },
  type: {
    el: (
      <>
        <Display>انسياب</Display>
        <Heading level={1} as="p">تقرير المبيعات السنوي</Heading>
        <Heading level={2} as="p">الربع الثالث</Heading>
        <Heading level={3} as="p">المنطقة الوسطى</Heading>
        <Heading level={4} as="p">ملاحظات الفريق</Heading>
      </>
    )
  },
  dividers: {
    el: (
      <>
        <Divider />
        <Divider label="أو" />
        <Divider start label="معلومات الشحن" />
        <Row className="ins-text-sm">
          <span>ملف</span>
          <Divider vertical />
          <span>تعديل</span>
          <Divider vertical />
          <span>عرض</span>
        </Row>
      </>
    )
  },
  validation: {
    el: (
      <Form validate className="ins-panel" id="v-form">
        <PanelHead title="حساب جديد" />
        <PanelBody className="ins-stack">
          <Field required label="الاسم" controlId="v-name">
            <Input autoComplete="name" />
          </Field>
          <Field required label="البريد الإلكتروني" controlId="v-email">
            <Input type="email" dir="ltr" autoComplete="email" placeholder="name@example.com" />
          </Field>
          <Field required label="كلمة المرور" controlId="v-pass" hint="ثمانية أحرف على الأقل." validationMessage="كلمة المرور قصيرة — ثمانية أحرف على الأقل.">
            <PasswordInput minLength={8} autoComplete="new-password" />
          </Field>
          <Check required label="أوافق على شروط الاستخدام" />
          <FormCommit>
            <Button variant="primary">إنشاء الحساب</Button>
          </FormCommit>
        </PanelBody>
      </Form>
    )
  },
  cols: {
    el: (
      <>
        <Cols n={12}>
          <div className="demo-box ins-span-8">span-8</div>
          <div className="demo-box ins-span-4">span-4</div>
        </Cols>
        <Cols n={12}>
          <div className="demo-box ins-span-3">span-3</div>
          <div className="demo-box ins-span-6">span-6</div>
          <div className="demo-box ins-span-3">span-3</div>
        </Cols>
        <Cols n={12}>
          <div className="demo-box ins-span-full">span-full</div>
        </Cols>
      </>
    )
  },
  accordion: {
    el: (
      <Panel bare>
        <Accordion>
          <Collapse name="billing" defaultOpen summary="الخطّة الحالية">خطّة النموّ، تتجدّد في الأوّل من تشرين الأوّل.</Collapse>
          <Collapse name="billing" summary="وسيلة الدفع">بطاقة تنتهي بـ <span className="ins-num">4242</span>.</Collapse>
          <Collapse name="billing" summary="الفواتير السابقة">اثنتا عشرة فاتورة، كلّها مدفوعة.</Collapse>
        </Accordion>
      </Panel>
    )
  },
  collapse: {
    el: (
      <Collapse summary="خيارات متقدّمة" bodyClassName="ins-stack">
        <Check label="إخفاء المنتج من محرّكات البحث" />
        <Check defaultChecked label="السماح بالتقييمات" />
      </Collapse>
    )
  },
  carousel: {
    el: (
      <Carousel perView={3} label="منتجات">
        <Card className="ins-p-5"><b>باقة الأعمال</b><p className="ins-muted">فواتير غير محدودة ومستخدمان.</p></Card>
        <Card className="ins-p-5"><b>باقة المتاجر</b><p className="ins-muted">ربط المتجر وتقارير المبيعات.</p></Card>
        <Card className="ins-p-5"><b>باقة الشركات</b><p className="ins-muted">فروع متعدّدة وصلاحيات مفصّلة.</p></Card>
        <Card className="ins-p-5"><b>باقة المحاسبين</b><p className="ins-muted">عملاء متعدّدون من حساب واحد.</p></Card>
        <Card className="ins-p-5"><b>باقة الجمعيات</b><p className="ins-muted">سندات القبض والتبرّعات.</p></Card>
      </Carousel>
    )
  },
  'theme-toggles': {
    el: (
      <>
        <ThemeToggle className="ins-btn ins-btn--secondary">{I('i-moon')}تبديل الوضع</ThemeToggle>
        <ThemeToggle mode="system" className="ins-btn ins-btn--ghost">اتبع النظام</ThemeToggle>
      </>
    )
  }
};
